using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CyberPolygon.Data;
using CyberPolygon.Data.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
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
                            .Include(i => i.Attachments) // Обязательно подтягиваем файлы
                            .OrderByDescending(i => i.Id)
                            .ToListAsync();
        }

        // 2. Добавление инструкции и загрузка списка файлов
        public async Task AddAsync(InstructionModel instruction, IReadOnlyList<IBrowserFile> files)
        {
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
                    // Генерируем уникальное имя файла, чтобы избежать перезаписи (Guid + оригинальное имя)
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.Name)}";
                    var fullPath = Path.Combine(uploadFolder, uniqueFileName);

                    // Сохраняем физический файл на диск сервера (Лимит 30 МБ)
                    await using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 30).CopyToAsync(fs);
                    }

                    // Создаем запись о вложении и добавляем в коллекцию инструкции
                    var attachment = new InstructionAttachment
                    {
                        FileName = file.Name, // Оригинальное имя для отображения пользователю
                        FilePath = $"/uploads/instructions/{uniqueFileName}" // Путь для скачивания
                    };

                    instruction.Attachments.Add(attachment);
                }
            }

            // Сохраняем инструкцию (EF Core автоматически сохранит и все объекты в Attachments)
            using var ctx = await _contextFactory.CreateDbContextAsync();
            ctx.Set<InstructionModel>().Add(instruction);
            await ctx.SaveChangesAsync();
        }

        // 3. Удаление инструкции и очистка диска от файлов
        public async Task DeleteAsync(int id)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();

            // Находим инструкцию вместе с ее файлами
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

                // Удаляем саму инструкцию из БД (EF Core каскадно удалит и записи из таблицы Attachments)
                ctx.Set<InstructionModel>().Remove(item);
                await ctx.SaveChangesAsync();
            }
        }
    }
}