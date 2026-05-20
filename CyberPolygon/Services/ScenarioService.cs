using CyberPolygon.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

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
        return await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);
    }

    // 2. Сохранить успешное прохождение
    public async Task SaveUserProgressAsync(string userId, int scenarioId, int score)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CyberScenarioId == scenarioId);

        if (progress == null)
        {
            context.UserProgresses.Add(new UserScenarioProgress
            {
                UserId = userId,
                CyberScenarioId = scenarioId,
                Score = score,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });
        }
        else
        {
            progress.Score = score;
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            context.UserProgresses.Update(progress);
        }
        await context.SaveChangesAsync();
    }

    // 3. АДМИН: Сбросить прогресс ВСЕХ пользователей для конкретного сценария
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

        return await context.UserProgresses
            .Where(p => p.CyberScenarioId == scenarioId && p.IsCompleted)
            .Join(context.Users, // Соединяем с таблицей пользователей Identity
                progress => progress.UserId, // Ключ из таблицы прогресса
                user => user.Id,             // Ключ из таблицы пользователей
                (progress, user) => new UserScenarioProgressDto // Проецируем в наш DTO
                {
                    Id = progress.Id,
                    UserId = progress.UserId,
                    UserName = user.UserName ?? "Без имени",
                    Email = user.Email ?? string.Empty,
                    Score = progress.Score,
                    IsCompleted = progress.IsCompleted,
                    CompletedAt = progress.CompletedAt
                })
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();
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

    public async Task<CyberScenario?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Scenarios
            .Include(s => s.Questions)
            .Include(s => s.Documents) // <-- ДОБАВЛЕНО
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}