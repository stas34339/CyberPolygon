using CyberPolygon.Data;
using CyberPolygon.Data.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace CyberPolygon.Services
{
    public class InstructionService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IWebHostEnvironment _env;

        public InstructionService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
        {
            _contextFactory = contextFactory;
            _env = env;
        }

        // 1. Получение всех инструкций вместе с прикрепленными файлами
        public async Task<List<InstructionModel>> GetAllAsync()
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Set<InstructionModel>()
                            .Include(i => i.Attachments)
                            .OrderBy(i => i.Order)
                            .ThenBy(i => i.Id)
                            .ToListAsync();
        }

        // 2. Добавление инструкции и загрузка списка файлов
        public async Task AddAsync(InstructionModel instruction, IReadOnlyList<IBrowserFile> files)
        {
            // Устанавливаем порядок (в конец списка)
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var maxOrder = await ctx.Set<InstructionModel>().MaxAsync(i => (int?)i.Order) ?? 0;
            instruction.Order = maxOrder + 1;

            // Папка для загрузки
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "instructions");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Обрабатываем каждый файл из списка
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.Name)}";
                    var fullPath = Path.Combine(uploadFolder, uniqueFileName);

                    await using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 30).CopyToAsync(fs);
                    }

                    var attachment = new InstructionAttachment
                    {
                        FileName = file.Name,
                        FilePath = $"/uploads/instructions/{uniqueFileName}"
                    };

                    instruction.Attachments.Add(attachment);
                }
            }

            ctx.Set<InstructionModel>().Add(instruction);
            await ctx.SaveChangesAsync();
        }

        // 3. Обновление инструкции (название, описание, иконка)
        public async Task UpdateAsync(InstructionModel updated)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var existing = await ctx.Set<InstructionModel>()
                .FirstOrDefaultAsync(i => i.Id == updated.Id);

            if (existing == null)
                throw new InvalidOperationException("Инструкция не найдена");

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.IconName = updated.IconName;

            await ctx.SaveChangesAsync();
        }

        // 4. Обновление только порядка инструкции
        public async Task UpdateOrderAsync(InstructionModel instruction)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var existing = await ctx.Set<InstructionModel>()
                .FirstOrDefaultAsync(i => i.Id == instruction.Id);

            if (existing == null)
                throw new InvalidOperationException("Инструкция не найдена");

            existing.Order = instruction.Order;
            await ctx.SaveChangesAsync();
        }

        // 5. Удаление инструкции и очистка диска от файлов
        public async Task DeleteAsync(int id)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();

            var item = await ctx.Set<InstructionModel>()
                                .Include(i => i.Attachments)
                                .FirstOrDefaultAsync(i => i.Id == id);

            if (item != null)
            {
                // Физически удаляем каждый файл с диска
                if (item.Attachments != null && item.Attachments.Any())
                {
                    foreach (var attachment in item.Attachments)
                    {
                        if (!string.IsNullOrEmpty(attachment.FilePath))
                        {
                            var fullPath = Path.Combine(_env.WebRootPath, attachment.FilePath.TrimStart('/'));
                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                            }
                        }
                    }
                }

                ctx.Set<InstructionModel>().Remove(item);
                await ctx.SaveChangesAsync();
            }
        }
    }
}