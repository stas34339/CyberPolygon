using CyberPolygon.Data;
using CyberPolygon.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberPolygon.Services
{
    public class InstructionService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public InstructionService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<InstructionModel>> GetAllAsync()
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Set<InstructionModel>()
                            .Include(i => i.Attachments)
                            .OrderBy(i => i.Order)
                            .ThenBy(i => i.Id)
                            .ToListAsync();
        }

        public async Task AddAsync(InstructionModel instruction)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var maxOrder = await ctx.Set<InstructionModel>().MaxAsync(i => (int?)i.Order) ?? 0;
            instruction.Order = maxOrder + 1;

            ctx.Set<InstructionModel>().Add(instruction);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(InstructionModel updated)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var existing = await ctx.Set<InstructionModel>()
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == updated.Id);

            if (existing == null) throw new InvalidOperationException("Инструкция не найдена");

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.IconName = updated.IconName;

            // Добавляем новые вложения, если они появились (Id == 0)
            foreach (var att in updated.Attachments.Where(a => a.Id == 0))
            {
                existing.Attachments.Add(att);
            }

            await ctx.SaveChangesAsync();
        }

        public async Task UpdateOrderAsync(InstructionModel instruction)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var existing = await ctx.Set<InstructionModel>().FirstOrDefaultAsync(i => i.Id == instruction.Id);
            if (existing != null)
            {
                existing.Order = instruction.Order;
                await ctx.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var item = await ctx.Set<InstructionModel>().Include(i => i.Attachments).FirstOrDefaultAsync(i => i.Id == id);
            if (item != null)
            {
                ctx.Set<InstructionModel>().Remove(item);
                await ctx.SaveChangesAsync();
            }
        }
    }
}