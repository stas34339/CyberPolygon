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
            foreach (var doc in test.Documents)
            {
                var filePath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            context.CyberTests.Remove(test);
            await context.SaveChangesAsync();
        }
    }

    public async Task<string> UploadDocumentAsync(IBrowserFile file)
    {
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", "tests");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var path = Path.Combine(folderPath, fileName);
        using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 15);
        using var fs = new FileStream(path, FileMode.Create);
        await stream.CopyToAsync(fs);
        return $"/uploads/tests/{fileName}";
    }

    // --- ЛОГИКА ПРОХОЖДЕНИЯ ---
    public async Task<UserTestProgress?> GetUserProgressAsync(string userId, int testId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserTestProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberTestId == testId);
    }

    public async Task StartTestAsync(string userId, int testId, int durationMinutes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.UserTestProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberTestId == testId);

        var now = DateTime.UtcNow;
        var endTime = durationMinutes > 0 ? now.AddMinutes(durationMinutes) : (DateTime?)null;

        if (existing == null)
        {
            context.UserTestProgresses.Add(new UserTestProgress
            {
                UserId = userId,
                CyberTestId = testId,
                Status = AttemptStatus.InProgress,
                StartedAt = now,
                TargetEndTime = endTime,
                Score = 0
            });
        }
        else if (existing.Status == AttemptStatus.NotStarted)
        {
            existing.Status = AttemptStatus.InProgress;
            existing.StartedAt = now; existing.TargetEndTime = endTime;
        }
        await context.SaveChangesAsync();
    }

    public async Task ProcessAnswerAsync(int progressId, int questionId, bool isCorrect)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserTestProgresses.FindAsync(progressId);
        if (progress == null || progress.Status != AttemptStatus.InProgress) return;

        var answer = await context.UserTestAnswers.FirstOrDefaultAsync(a => a.UserTestProgressId == progressId && a.QuestionId == questionId);
        if (answer == null)
        {
            context.UserTestAnswers.Add(new UserTestAnswer { UserTestProgressId = progressId, QuestionId = questionId, IsCorrect = isCorrect });
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
        return await context.UserTestProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.CyberTestId, p => p.Status);
    }
}