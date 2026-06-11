using CyberPolygon.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using CyberPolygon.Services;

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

    // ИСПРАВЛЕННЫЙ МЕТОД СОХРАНЕНИЯ СЦЕНАРИЯ (СОХРАНЯЕТ ВСЕ ВОПРОСЫ СРАЗУ)
    public async Task SaveAsync(CyberScenario scenario)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        if (scenario.Id == 0)
        {
            // Если сценарий новый, EF Core автоматически добавит и сценарий, и все привязанные к нему вопросы
            context.Scenarios.Add(scenario);
        }
        else
        {
            var existingScenario = await context.Scenarios
                .Include(s => s.Questions)
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == scenario.Id);

            if (existingScenario != null)
            {
                context.Entry(existingScenario).CurrentValues.SetValues(scenario);
                existingScenario.GameMode = scenario.GameMode;

                // Синхронизация коллекции вопросов
                foreach (var existingQuestion in existingScenario.Questions.ToList())
                {
                    if (!scenario.Questions.Any(q => q.Id == existingQuestion.Id))
                        context.Remove(existingQuestion);
                }

                foreach (var q in scenario.Questions)
                {
                    if (q.Id == 0)
                    {
                        existingScenario.Questions.Add(q);
                    }
                    else
                    {
                        var existingQ = existingScenario.Questions.FirstOrDefault(eq => eq.Id == q.Id);
                        if (existingQ != null)
                            context.Entry(existingQ).CurrentValues.SetValues(q);
                    }
                }

                // Синхронизация документов
                foreach (var existingDoc in existingScenario.Documents.ToList())
                {
                    if (!scenario.Documents.Any(d => d.Id == existingDoc.Id))
                        context.Remove(existingDoc);
                }
                foreach (var d in scenario.Documents)
                {
                    if (d.Id == 0)
                    {
                        existingScenario.Documents.Add(d);
                    }
                    else
                    {
                        var existingD = existingScenario.Documents.FirstOrDefault(ed => ed.Id == d.Id);
                        if (existingD != null)
                            context.Entry(existingD).CurrentValues.SetValues(d);
                    }
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
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", "schemas");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var path = Path.Combine(folderPath, fileName);
        using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5);
        using var fs = new FileStream(path, FileMode.Create);
        await stream.CopyToAsync(fs);
        return $"/uploads/schemas/{fileName}";
    }

    public async Task<UserScenarioProgress?> GetUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        if (scenario != null && scenario.Scope == VisibilityScope.TeamOnly && scenario.TargetTeamId.HasValue)
        {
            return await context.UserProgresses
                .FirstOrDefaultAsync(p => p.CyberScenarioId == scenarioId && p.TeamId == scenario.TargetTeamId.Value && p.IsTeamAttempt);
        }

        return await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId && !p.IsTeamAttempt);
    }

    public async Task StartScenarioAsync(string userId, int scenarioId, int durationMinutes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        bool isTeamMode = scenario?.Scope == VisibilityScope.TeamOnly;
        int? teamId = isTeamMode ? scenario?.TargetTeamId : null;
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p =>
            p.CyberScenarioId == scenarioId &&
            ((isTeamMode && p.TeamId == teamId && p.IsTeamAttempt) || (!isTeamMode && p.UserId == userId && !p.IsTeamAttempt))
        );
        var now = DateTime.UtcNow;
        var endTime = durationMinutes > 0 ? now.AddMinutes(durationMinutes) : (DateTime?)null;
        if (progress == null)
        {
            context.UserProgresses.Add(new UserScenarioProgress
            {
                UserId = isTeamMode ? null : userId,
                TeamId = teamId,
                IsTeamAttempt = isTeamMode,
                CyberScenarioId = scenarioId,
                Status = AttemptStatus.InProgress,
                StartedAt = now,
                TargetEndTime = endTime,
                Score = 0
            });
        }
        else
        {
            progress.Status = AttemptStatus.InProgress;
            progress.StartedAt = now;
            progress.TargetEndTime = endTime;
            progress.Score = 0;
            progress.CompletedAt = null;
            progress.TimeSpent = null;
            context.UserProgresses.Update(progress);
        }

        await context.SaveChangesAsync();
    }

    public async Task CompleteUserProgressAsync(string userId, int scenarioId, int score)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);
        if (progress != null && progress.Status == AttemptStatus.InProgress)
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
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);
        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Status = AttemptStatus.Failed;
            progress.CompletedAt = DateTime.UtcNow;
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
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

    public async Task ResetUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var record = await context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);
        if (record != null)
        {
            context.UserProgresses.Remove(record);
            await context.SaveChangesAsync();
        }
    }

    public async Task ResetProgressByIdAsync(int progressId)
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

    // ИСПРАВЛЕННЫЙ МЕТОД: ПРИНИМАЕТ ЛЮБОЕ КОЛИЧЕСТВО АРГУМЕНТОВ И ИСПОЛЬЗУЕТ ВЛОЖЕННЫЙ ТИП
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

        // Проверка через умный валидатор
        bool isCorrect = AnswerValidator.Validate(question.CorrectAnswer, userAnswer);

        if (answerRecord == null)
        {
            // Явное указание вложенного типа контекста БД
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