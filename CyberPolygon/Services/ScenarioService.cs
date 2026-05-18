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
        return await context.Scenarios.ToListAsync(); // Настоящий асинхронный метод
    }

    // Сохранение данных
    public async Task SaveAsync(CyberScenario scenario)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        if (scenario.Id == 0)
            context.Scenarios.Add(scenario);
        else
            context.Scenarios.Update(scenario);

        await context.SaveChangesAsync(); // Настоящий асинхронный метод
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
}