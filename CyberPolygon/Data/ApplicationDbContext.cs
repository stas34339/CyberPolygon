using CyberPolygon.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace CyberPolygon.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Строка должна быть ЗДЕСЬ (внутри класса)
        public DbSet<CyberScenario> Scenarios { get; set; }
        public DbSet<ScenarioQuestion> ScenarioQuestions { get; set; }
        public DbSet<UserScenarioProgress> UserProgresses { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserTeam> UserTeams { get; set; }
        // Добавь это свойство к остальным DbSet
        public DbSet<UserAnswerProgress> UserAnswerProgresses { get; set; }
        public DbSet<InstructionModel> Instructions { get; set; }

       // Добавление таблиц для тестов
        public DbSet<CyberTest> CyberTests { get; set; }
        public DbSet<TestQuestion> TestQuestions { get; set; }
        public DbSet<TestOption> TestOptions { get; set; }
        public DbSet<TestDocument> TestDocuments { get; set; }
        public DbSet<UserTestProgress> UserTestProgresses { get; set; }
        public DbSet<UserTestAnswer> UserTestAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CyberScenario>().ToTable("Scenarios");

            // Настройка связи "Один ко многим" для Группы и Пользователей
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Group)
                .WithMany(g => g.Users)
                .HasForeignKey(u => u.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связи "Многие ко многим" для Команд и Пользователей через авто-таблицу
            builder.Entity<UserTeam>()
                .HasMany(t => t.Users)
                .WithMany(u => u.Teams)
                .UsingEntity(j => j.ToTable("UserTeamMappings"));
        }
        public class UserAnswerProgress
        {
            public int Id { get; set; }
            public int UserScenarioProgressId { get; set; } // Привязка к командной сессии
            public int QuestionId { get; set; }             // Привязка к конкретному вопросу

            public bool IsCorrect { get; set; } = false;
            public int FailedAttempts { get; set; } = 0;

            public string LastSubmittedAnswer { get; set; } = string.Empty;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

    }
}