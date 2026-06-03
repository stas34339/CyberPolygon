using CyberPolygon.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using static CyberPolygon.Data.ApplicationDbContext;

public class ScenarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory; // Меняем на фабрику
    private readonly IWebHostEnvironment _env;

    public ScenarioService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        _env = env;
    }

    // Получение данных с использованием отдельного контекста
    public async Task<List<CyberScenario>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Scenarios
            .Include(s => s.Questions)
            .Include(s => s.Documents) // <-- ДОБАВЛЕНО
            .ToListAsync();
    }

    // Сохранение данных
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
                .Include(s => s.Documents) // <-- ОБЯЗАТЕЛЬНО ПОДТЯГИВАЕМ СУЩЕСТВУЮЩИЕ ДОКУМЕНТЫ
                .FirstOrDefaultAsync(s => s.Id == scenario.Id);

            if (existingScenario != null)
            {
                context.Entry(existingScenario).CurrentValues.SetValues(scenario);
                existingScenario.GameMode = scenario.GameMode;

                // --- СИНХРОНИЗАЦИЯ ВОПРОСОВ (Твой рабочий код) ---
                foreach (var existingQuestion in existingScenario.Questions.ToList())
                {
                    if (!scenario.Questions.Any(q => q.Id == existingQuestion.Id))
                        context.Remove(existingQuestion);
                }
                foreach (var q in scenario.Questions)
                {
                    var existingQ = existingScenario.Questions.FirstOrDefault(eq => eq.Id == q.Id);
                    if (existingQ == null) existingScenario.Questions.Add(q);
                    else context.Entry(existingQ).CurrentValues.SetValues(q);
                }

                // --- СИНХРОНИЗАЦИЯ ДОКУМЕНТОВ (ДОБАВЛЕНО, ЧТОБЫ НЕ ИСЧЕЗАЛИ ФАЙЛЫ) ---
                foreach (var existingDoc in existingScenario.Documents.ToList())
                {
                    if (!scenario.Documents.Any(d => d.Id == existingDoc.Id))
                        context.Remove(existingDoc); // Удаляем из БД, если админ удалил на форме
                }
                foreach (var d in scenario.Documents)
                {
                    var existingD = existingScenario.Documents.FirstOrDefault(ed => ed.Id == d.Id);
                    if (existingD == null)
                    {
                        existingScenario.Documents.Add(d); // Добавляем новый документ
                    }
                    else
                    {
                        context.Entry(existingD).CurrentValues.SetValues(d); // Обновляем старый (например, название)
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

    // Удаление данных
    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.Scenarios.FindAsync(id); // Настоящий асинхронный метод
        if (item != null)
        {
            // Удаляем файл с диска, если он есть
            if (!string.IsNullOrEmpty(item.SchemaPath))
            {
                var filePath = Path.Combine(_env.WebRootPath, item.SchemaPath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            context.Scenarios.Remove(item);
            await context.SaveChangesAsync(); // Настоящий асинхронный метод
        }
    }

    // Метод для загрузки картинки (остается без изменений, контекст не использует)
    public async Task<string> UploadSchemaAsync(IBrowserFile file)
    {
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", "schemas");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var path = Path.Combine(folderPath, fileName);

        using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5); // 5MB max
        using var fs = new FileStream(path, FileMode.Create);
        await stream.CopyToAsync(fs);

        return $"/uploads/schemas/{fileName}";
    }

    // 1. Получить прогресс конкретного пользователя
    public async Task<UserScenarioProgress?> GetUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        // Если сценарий назначен на команду - ищем общую командную сессию
        if (scenario != null && scenario.Scope == VisibilityScope.TeamOnly && scenario.TargetTeamId.HasValue)
        {
            return await context.UserProgresses
                .FirstOrDefaultAsync(p => p.CyberScenarioId == scenarioId
                                       && p.TeamId == scenario.TargetTeamId.Value
                                       && p.IsTeamAttempt);
        }

        // Иначе ищем личную сессию
        return await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId && !p.IsTeamAttempt);
    }

    public async Task StartScenarioAsync(string userId, int scenarioId, int durationMinutes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var scenario = await context.Scenarios.FindAsync(scenarioId);

        bool isTeamMode = scenario?.Scope == VisibilityScope.TeamOnly;
        int? teamId = isTeamMode ? scenario?.TargetTeamId : null;

        // ВАЖНО: Ищем прогресс (он может быть уже создан другим членом команды)
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
                UserId = isTeamMode ? null : userId, // Для команды UserId можно не писать, важен TeamId
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
        var progress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);

        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Score = score;
            progress.Status = AttemptStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            if (progress.StartedAt.HasValue)
            {
                progress.TimeSpent = progress.CompletedAt.Value - progress.StartedAt.Value;
            }
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
    }

    public async Task FailUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);

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




    // АДМИН: Точечный сброс прогресса одного пользователя
    public async Task ResetUserProgressAsync(string userId, int scenarioId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var record = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);

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
    public async Task<(bool Success, bool AlreadySolved, int NewScore)> ProcessTeamAnswerAsync(int progressId, int questionId, bool isCorrect, int awardPoints, int penaltyPoints)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FindAsync(progressId);

        if (progress == null || progress.Status != AttemptStatus.InProgress)
            return (false, false, 0);

        // Проверяем, не решил ли этот вопрос кто-то другой из команды миллисекунду назад
        bool alreadySolved = await context.UserAnswerProgresses
            .AnyAsync(a => a.UserScenarioProgressId == progressId && a.QuestionId == questionId);

        if (alreadySolved)
            return (false, true, progress.Score); // Вопрос уже решен, очки не начисляем

        if (isCorrect)
        {
            context.UserAnswerProgresses.Add(new UserAnswerProgress
            {
                UserScenarioProgressId = progressId,
                QuestionId = questionId
            });
            progress.Score += awardPoints;
        }
        else
        {
            progress.Score -= penaltyPoints;
            // Опционально: if (progress.Score < 0) progress.Score = 0; // Не даем уйти в минус
        }

        await context.SaveChangesAsync();
        return (true, false, progress.Score);
    }

    // 2. Получение списка решенных задач для подсветки зеленым в UI
    public async Task<List<int>> GetCompletedQuestionsAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserAnswerProgresses
            .Where(a => a.UserScenarioProgressId == progressId)
            .Select(a => a.QuestionId)
            .ToListAsync();
    }

    // 3. Командное завершение сценария (работает по ID сессии, а не ID юзера)
    public async Task CompleteProgressByIdAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FindAsync(progressId);

        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Status = AttemptStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            if (progress.StartedAt.HasValue)
            {
                progress.TimeSpent = progress.CompletedAt.Value - progress.StartedAt.Value;
            }
            context.UserProgresses.Update(progress);
            await context.SaveChangesAsync();
        }
    }

}