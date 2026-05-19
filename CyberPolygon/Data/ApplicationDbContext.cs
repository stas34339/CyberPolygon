using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace CyberPolygon.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Строка должна быть ЗДЕСЬ (внутри класса)
        public DbSet<CyberScenario> Scenarios { get; set; }
        public DbSet<ScenarioQuestion> ScenarioQuestions { get; set; }

        public DbSet<UserScenarioProgress> UserProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Здесь можно настроить таблицу, если нужно, например:
            builder.Entity<CyberScenario>().ToTable("Scenarios");
        }
    }
}