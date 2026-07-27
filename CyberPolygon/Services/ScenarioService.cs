// ScenarioService.cs
using CyberPolygon.Data;
using CyberPolygon.Models;
using CyberPolygon.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

public class ScenarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWebHostEnvironment _env;

    public ScenarioService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        _env = env;
    }

    public async Task<List<CyberScenario>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Scenarios
            .Include(s => s.Questions)
            .Include(s => s.Documents)
            .ToListAsync();
    }

    public async Task SaveAsync(CyberScenario scenario)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        if (scenario.Id == 0)
        {
            context.Scenarios.Add(scenario);
        }
        else
        {
            var existingScenario = await context.Scenarios
                .Include(s => s.Questions)
                .Include(s => s.Documents)
                .Include(s => s.Devices)
                    .ThenInclude(d => d.Applications) // Учим сервис видеть приложения
                .Include(s => s.Connections)
                .FirstOrDefaultAsync(s => s.Id == scenario.Id);

            if (existingScenario != null)
            {
                context.Entry(existingScenario).CurrentValues.SetValues(scenario);
                existingScenario.GameMode = scenario.GameMode;

                // 1. Сохранение вопросов
                context.RemoveRange(existingScenario.Questions.Where(eq => !scenario.Questions.Any(q => q.Id == eq.Id)));
                foreach (var q in scenario.Questions)
                {
                    if (q.Id == 0) existingScenario.Questions.Add(q);
                    else context.Entry(existingScenario.Questions.First(eq => eq.Id == q.Id)).CurrentValues.SetValues(q);
                }

                // 2. Сохранение документов
                context.RemoveRange(existingScenario.Documents.Where(ed => !scenario.Documents.Any(d => d.Id == ed.Id)));
                foreach (var d in scenario.Documents)
                {
                    if (d.Id == 0) existingScenario.Documents.Add(d);
                    else context.Entry(existingScenario.Documents.First(ed => ed.Id == d.Id)).CurrentValues.SetValues(d);
                }

                // 3. СОХРАНЕНИЕ ТОПОЛОГИИ И ПРИЛОЖЕНИЙ
                context.RemoveRange(existingScenario.Devices.Where(ed => !scenario.Devices.Any(d => d.Id == ed.Id)));
                foreach (var d in scenario.Devices)
                {
                    if (d.Id == 0)
                    {
                        existingScenario.Devices.Add(d);
                    }
                    else
                    {
                        var existingD = existingScenario.Devices.First(ed => ed.Id == d.Id);
                        context.Entry(existingD).CurrentValues.SetValues(d);

                        // Сохранение приложений внутри ПК
                        existingD.Applications ??= new List<DeviceApplication>();
                        d.Applications ??= new List<DeviceApplication>();

                        context.RemoveRange(existingD.Applications.Where(ea => !d.Applications.Any(a => a.Id == ea.Id)));
                        foreach (var a in d.Applications)
                        {
                            if (a.Id == 0) existingD.Applications.Add(a);
                            else context.Entry(existingD.Applications.First(ea => ea.Id == a.Id)).CurrentValues.SetValues(a);
                        }
                    }
                }

                // 4. Сохранение связей (кабелей)
                context.RemoveRange(existingScenario.Connections.Where(ec => !scenario.Connections.Any(c => c.Id == ec.Id)));
                foreach (var c in scenario.Connections)
                {
                    if (c.Id == 0) existingScenario.Connections.Add(c);
                    else context.Entry(existingScenario.Connections.First(ec => ec.Id == c.Id)).CurrentValues.SetValues(c);
                }
            }
            else
            {
                context.Scenarios.Update(scenario);
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        // Твоя реализация DeleteAsync без изменений
        using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.Scenarios.FindAsync(id);
        if (item != null)
        {
            if (!string.IsNullOrEmpty(item.SchemaPath))
            {
                var filePath = Path.Combine(_env.WebRootPath, item.SchemaPath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            context.Scenarios.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    public async Task<string> UploadSchemaAsync(IBrowserFile file)
    {
        // Твоя реализация UploadSchemaAsync без изменений
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", "schemas");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var path = Path.Combine(folderPath, fileName);
        using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5);
        using var fs = new FileStream(path, FileMode.Create);
        await stream.CopyToAsync(fs);
        return $"/uploads/schemas/{fileName}";
    }

    // Возвращает АКТИВНУЮ или ПОСЛЕДНЮЮ попытку для отображения в UI
    public async Task<UserScenarioProgress?> GetUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        // Для командного режима ищем активную попытку команды
        if (scenario != null && scenario.Scope == VisibilityScope.TeamOnly && scenario.TargetTeamId.HasValue)
        {
            return await context.UserProgresses
                .Where(p => p.CyberScenarioId == scenarioId && p.TeamId == scenario.TargetTeamId.Value && p.IsTeamAttempt)
                .OrderByDescending(p => p.StartedAt) // Сначала активные (InProgress), потом последние завершенные
                .FirstOrDefaultAsync();
        }

        // Для персонального режима
        return await context.UserProgresses
            .Where(p => p.UserId == userId && p.CyberScenarioId == scenarioId && !p.IsTeamAttempt)
            .OrderByDescending(p => p.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<UserScenarioProgress> StartScenarioAsync(string userId, int scenarioId, int durationMinutes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        bool isTeamMode = scenario?.Scope == VisibilityScope.TeamOnly;
        int? teamId = isTeamMode ? scenario?.TargetTeamId : null;

        // Ищем существующую активную попытку
        var existingProgress = await context.UserProgresses.FirstOrDefaultAsync(p =>
            p.CyberScenarioId == scenarioId &&
            ((isTeamMode && p.TeamId == teamId && p.IsTeamAttempt) || (!isTeamMode && p.UserId == userId && !p.IsTeamAttempt)) &&
            p.Status == AttemptStatus.InProgress
        );

        // Если уже есть активная, возвращаем её
        if (existingProgress != null)
        {
            return existingProgress;
        }

        // Иначе создаем новую
        var now = DateTime.UtcNow;
        var endTime = durationMinutes > 0 ? now.AddMinutes(durationMinutes) : (DateTime?)null;

        var newProgress = new UserScenarioProgress
        {
            UserId = isTeamMode ? null : userId,
            TeamId = teamId,
            IsTeamAttempt = isTeamMode,
            CyberScenarioId = scenarioId,
            Status = AttemptStatus.InProgress,
            StartedAt = now,
            TargetEndTime = endTime,
            Score = 0
        };

        context.UserProgresses.Add(newProgress);
        await context.SaveChangesAsync();
        return newProgress;
    }

    public async Task CompleteUserProgressAsync(string userId, int scenarioId, int score)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId && p.Status == AttemptStatus.InProgress);
        if (progress != null)
        {
            progress.Score = score;
            progress.Status = AttemptStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            if (progress.StartedAt.HasValue) progress.TimeSpent = progress.CompletedAt.Value - progress.StartedAt.Value;
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
    }

    public async Task FailUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId && p.Status == AttemptStatus.InProgress);
        if (progress != null)
        {
            progress.Status = AttemptStatus.Failed;
            progress.CompletedAt = DateTime.UtcNow;
            if (progress.StartedAt.HasValue) progress.TimeSpent = progress.CompletedAt.Value - progress.StartedAt.Value;
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
    }

    // НОВЫЙ МЕТОД: Создает новую попытку, оставляя старую в истории
    public async Task<UserScenarioProgress> RestartUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);
        if (scenario == null) throw new InvalidOperationException("Сценарий не найден");

        bool isTeamMode = scenario.Scope == VisibilityScope.TeamOnly;
        int? teamId = isTeamMode ? scenario.TargetTeamId : null;

        // Помечаем все незавершенные попытки как Failed, если они есть (на всякий случай)
        var activeProgresses = await context.UserProgresses
            .Where(p => p.CyberScenarioId == scenarioId &&
                   ((isTeamMode && p.TeamId == teamId && p.IsTeamAttempt) || (!isTeamMode && p.UserId == userId && !p.IsTeamAttempt)) &&
                   p.Status == AttemptStatus.InProgress)
            .ToListAsync();

        foreach (var p in activeProgresses)
        {
            p.Status = AttemptStatus.Failed;
            p.CompletedAt = DateTime.UtcNow;
        }

        // Создаем новую попытку в статусе NotStarted
        var newProgress = new UserScenarioProgress
        {
            UserId = isTeamMode ? null : userId,
            TeamId = teamId,
            IsTeamAttempt = isTeamMode,
            CyberScenarioId = scenarioId,
            Status = AttemptStatus.NotStarted,
            Score = 0
        };

        context.UserProgresses.Add(newProgress);
        await context.SaveChangesAsync();
        return newProgress;
    }

    public async Task ResetAllProgressForScenarioAsync(int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var records = await context.UserProgresses.Where(p => p.CyberScenarioId == scenarioId).ToListAsync();
        if (records.Any())
        {
            context.UserProgresses.RemoveRange(records);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<UserScenarioProgressDto>> GetAllProgressForScenarioAsync(int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progresses = await context.UserProgresses
            .Where(p => p.CyberScenarioId == scenarioId)
            .OrderByDescending(p => p.StartedAt)
            .ToListAsync();
        var result = new List<UserScenarioProgressDto>();

        foreach (var p in progresses)
        {
            var dto = new UserScenarioProgressDto
            {
                Id = p.Id,
                Score = p.Score,
                Status = p.Status,
                CompletedAt = p.CompletedAt,
                TimeSpent = p.TimeSpent,
                IsTeamAttempt = p.IsTeamAttempt,
                TeamId = p.TeamId,
                UserId = p.UserId ?? string.Empty
            };
            if (p.IsTeamAttempt && p.TeamId.HasValue)
            {
                var team = await context.UserTeams.FindAsync(p.TeamId.Value);
                dto.TeamName = team?.Name ?? "Неизвестная команда";
                dto.UserName = "Командная сессия";
            }
            else if (!string.IsNullOrEmpty(p.UserId))
            {
                var user = await context.Users.FindAsync(p.UserId);
                dto.UserName = user?.UserName ?? "Без имени";
                dto.Email = user?.Email ?? string.Empty;
            }
            result.Add(dto);
        }
        return result;
    }

    // Удаляет конкретную попытку (для админа)
    public async Task DeleteProgressByIdAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var record = await context.UserProgresses.FindAsync(progressId);
        if (record != null)
        {
            context.UserProgresses.Remove(record);
            await context.SaveChangesAsync();
        }
    }

    public async Task<CyberScenario?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Scenarios
            .Include(s => s.Questions)
            .Include(s => s.Documents)
            .Include(s => s.Devices)
            .ThenInclude(d => d.Applications)
            .Include(s => s.Connections) // <--- ВОТ ЭТА СТРОКА ВКЛЮЧИТ ЛИНИИ В ТЕРМИНАЛЕ!
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task UpdateScoreAsync(int progressId, int newScore)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FindAsync(progressId);
        if (progress != null)
        {
            progress.Score = newScore;
            await context.SaveChangesAsync();
        }
    }

    public async Task<(bool Success, bool AlreadySolved, int EarnedPoints, int NewTotalScore)> ProcessTeamAnswerAsync(
        int progressId, int questionId, string userAnswer, object? arg4 = null, object? arg5 = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FindAsync(progressId);
        var question = await context.ScenarioQuestions.FindAsync(questionId);

        if (progress == null || question == null || progress.Status != AttemptStatus.InProgress)
            return (false, false, 0, progress?.Score ?? 0);

        var answerRecord = await context.UserAnswerProgresses
            .FirstOrDefaultAsync(a => a.UserScenarioProgressId == progressId && a.QuestionId == questionId);

        if (answerRecord != null && answerRecord.IsCorrect)
            return (true, true, 0, progress.Score);

        bool isCorrect = AnswerValidator.Validate(question.CorrectAnswer, userAnswer);

        if (answerRecord == null)
        {
            answerRecord = new ApplicationDbContext.UserAnswerProgress
            {
                UserScenarioProgressId = progressId,
                QuestionId = questionId,
                FailedAttempts = isCorrect ? 0 : 1,
                IsCorrect = isCorrect,
                LastSubmittedAnswer = userAnswer ?? "",
                UpdatedAt = DateTime.UtcNow
            };
            context.UserAnswerProgresses.Add(answerRecord);
        }
        else
        {
            answerRecord.IsCorrect = isCorrect;
            answerRecord.LastSubmittedAnswer = userAnswer ?? "";
            answerRecord.UpdatedAt = DateTime.UtcNow;
            if (!isCorrect) answerRecord.FailedAttempts++;
        }

        int earnedPoints = 0;
        if (isCorrect)
        {
            earnedPoints = Math.Max(0, question.AwardPoints - (answerRecord.FailedAttempts * question.PenaltyPoints));
            progress.Score += earnedPoints;
        }

        await context.SaveChangesAsync();
        return (isCorrect, false, earnedPoints, progress.Score);
    }

    public async Task<Dictionary<int, (bool IsCorrect, int FailedAttempts)>> GetUserQuestionStatesAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var records = await context.UserAnswerProgresses
            .Where(a => a.UserScenarioProgressId == progressId)
            .Select(a => new { a.QuestionId, a.IsCorrect, a.FailedAttempts })
            .ToListAsync();
        return records.ToDictionary(r => r.QuestionId, r => (r.IsCorrect, r.FailedAttempts));
    }

    public async Task CompleteProgressByIdAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FindAsync(progressId);

        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Status = AttemptStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            if (progress.StartedAt.HasValue) progress.TimeSpent = progress.CompletedAt.Value - progress.StartedAt.Value;
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
    }
}