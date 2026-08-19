using CyberPolygon.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

public class TestService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWebHostEnvironment _env;

    public TestService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        _env = env;
    }

    public async Task<List<CyberTest>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CyberTests
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .Include(t => t.Documents)
            .ToListAsync();
    }

    public async Task<CyberTest?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CyberTests
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task SaveAsync(CyberTest test)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        if (test.Id == 0)
        {
            context.CyberTests.Add(test);
        }
        else
        {
            var existing = await context.CyberTests
                .Include(t => t.Questions).ThenInclude(q => q.Options)
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == test.Id);

            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(test);

                // Синхронизация вопросов (EF Core каскадно удалит старые, если настроено, или делаем вручную)
                context.RemoveRange(existing.Questions);
                existing.Questions = test.Questions;

                context.RemoveRange(existing.Documents);
                existing.Documents = test.Documents;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var test = await context.CyberTests.Include(t => t.Documents).FirstOrDefaultAsync(t => t.Id == id);
        if (test != null)
        {
            // Файлы хранятся в БД вместе с тестом и удалятся каскадно — отдельная чистка диска не нужна.
            context.CyberTests.Remove(test);
            await context.SaveChangesAsync();
        }
    }

    // --- ЛОГИКА ПРОХОЖДЕНИЯ ---
    public async Task<UserTestProgress?> GetUserProgressAsync(string userId, int testId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Сначала ищем активную попытку
        var active = await context.UserTestProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberTestId == testId && p.Status == AttemptStatus.InProgress);

        if (active != null) return active;

        // Если нет активной — возвращаем последнюю завершенную
        return await context.UserTestProgresses
            .Where(p => p.UserId == userId && p.CyberTestId == testId)
            .OrderByDescending(p => p.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task StartTestAsync(string userId, int testId, int durationMinutes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Ищем незавершенную попытку
        var existing = await context.UserTestProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberTestId == testId && p.Status == AttemptStatus.InProgress);

        if (existing != null)
        {
            // Уже есть активная попытка — не создаем новую
            return;
        }

        var now = DateTime.UtcNow;
        var endTime = durationMinutes > 0 ? now.AddMinutes(durationMinutes) : (DateTime?)null;

        context.UserTestProgresses.Add(new UserTestProgress
        {
            UserId = userId,
            CyberTestId = testId,
            Status = AttemptStatus.InProgress,
            StartedAt = now,
            TargetEndTime = endTime,
            Score = 0
        });

        await context.SaveChangesAsync();
    }

    public async Task ProcessAnswerAsync(int progressId, int questionId, bool isCorrect, string selectedAnswer)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress == null || progress.Status != AttemptStatus.InProgress) return;

        var answer = await context.UserTestAnswers.FirstOrDefaultAsync(a => a.UserTestProgressId == progressId && a.QuestionId == questionId);
        if (answer == null)
        {
            context.UserTestAnswers.Add(new UserTestAnswer
            {
                UserTestProgressId = progressId,
                QuestionId = questionId,
                IsCorrect = isCorrect,
                SelectedAnswer = selectedAnswer,
                AnsweredAt = DateTime.UtcNow
            });
            if (isCorrect) progress.Score++;
            await context.SaveChangesAsync();
        }
    }

    public async Task CompleteTestAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Status = AttemptStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<int>> GetAnsweredCorrectlyAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserTestAnswers
            .Where(a => a.UserTestProgressId == progressId && a.IsCorrect)
            .Select(a => a.QuestionId)
            .ToListAsync();
    }

    public async Task<List<int>> GetAnsweredWrongAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserTestAnswers
            .Where(a => a.UserTestProgressId == progressId && !a.IsCorrect)
            .Select(a => a.QuestionId)
            .ToListAsync();
    }
    public async Task<Dictionary<int, AttemptStatus>> GetAllUserProgressesAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var progresses = await context.UserTestProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync();

        // Для каждого теста определяем результирующий статус:
        // - Если есть Completed — показываем Completed
        // - Если есть InProgress — показываем InProgress  
        // - Иначе — последний статус (обычно NotStarted)
        return progresses
            .GroupBy(p => p.CyberTestId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(p => p.StartedAt).First();
                var hasCompleted = g.Any(p => p.Status == AttemptStatus.Completed);
                var hasInProgress = g.Any(p => p.Status == AttemptStatus.InProgress);

                AttemptStatus status;
                if (hasInProgress)
                    status = AttemptStatus.InProgress;
                else if (hasCompleted)
                    status = AttemptStatus.Completed;
                else
                    status = latest.Status;

                return new { TestId = g.Key, Status = status };
            })
            .ToDictionary(x => x.TestId, x => x.Status);
    }
    public async Task<UserTestProgress> RestartTestAsync(string userId, int testId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var test = await context.CyberTests.FindAsync(testId);
        if (test == null) throw new InvalidOperationException("Тест не найден");

        var now = DateTime.UtcNow;
        var endTime = test.DurationInMinutes > 0 ? now.AddMinutes(test.DurationInMinutes) : (DateTime?)null;

        // Создаем новую попытку и сразу активируем её
        var newProgress = new UserTestProgress
        {
            UserId = userId,
            CyberTestId = testId,
            Status = AttemptStatus.InProgress,
            StartedAt = now,
            TargetEndTime = endTime,
            Score = 0
        };
        context.UserTestProgresses.Add(newProgress);
        await context.SaveChangesAsync();

        return newProgress;
    }
    public async Task RequestRetakeAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress != null) { progress.IsRetakeRequested = true; await context.SaveChangesAsync(); }
    }
    public async Task GrantRetakeAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress != null) { progress.IsRetakeRequested = false; progress.IsRetakeGranted = true; await context.SaveChangesAsync(); }
    }
    public async Task RejectRetakeAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress != null) { progress.IsRetakeRequested = false; progress.IsRetakeGranted = false; await context.SaveChangesAsync(); }
    }
}