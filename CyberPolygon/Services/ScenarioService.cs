using CyberPolygon.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

public class ScenarioService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ScenarioService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<List<CyberScenario>> GetAllAsync() =>  _context.Scenarios.ToList();

    public async Task SaveAsync(CyberScenario scenario)
    {
        if (scenario.Id == 0) _context.Scenarios.Add(scenario);
        else _context.Scenarios.Update(scenario);

         _context.SaveChanges();
    }

    public async Task DeleteAsync(int id)
    {
        var item =  _context.Scenarios.Find(id);
        if (item != null)
        {
            // Удаляем файл с диска, если он есть
            if (!string.IsNullOrEmpty(item.SchemaPath))
            {
                var filePath = Path.Combine(_env.WebRootPath, item.SchemaPath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            _context.Scenarios.Remove(item);
             _context.SaveChanges();
        }
    }

    // Метод для загрузки картинки
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