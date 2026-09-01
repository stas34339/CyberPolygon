using CyberPolygon.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ScenarioSessionManager : IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private Timer? _backgroundTimer;

    // Событие для Real-time обновления страниц воркспейса у команд
    public event Action<int>? TeamProgressChanged; // Параметр: TeamId
    // Событие для обновления индивидуальных экранов
    public event Action<string>? UserProgressChanged; // Параметр: UserId
    // Событие для обновления админ-панели (списка живых сессий)
    public event Action? ActiveSessionsChanged;

    public ScenarioSessionManager(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        // Запускаем фоновый сборщик просроченных сессий каждые 15 секунд
        _backgroundTimer = new Timer(CheckExpiredSessions, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }

    private async void CheckExpiredSessions(object? state)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // 1. Ищем просроченные симуляции (Сценарии)
            var expiredScenarios = await context.UserProgresses
                .Where(p => p.Status == AttemptStatus.InProgress
                         && p.TargetEndTime.HasValue
                         && p.TargetEndTime.Value <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredScenarios.Any())
            {
                foreach (var p in expiredScenarios)
                {
                    p.Status = AttemptStatus.Failed;
                    p.CompletedAt = DateTime.UtcNow;
                    if (p.StartedAt.HasValue)
                        p.TimeSpent = p.CompletedAt.Value - p.StartedAt.Value;
                }
                context.UserProgresses.UpdateRange(expiredScenarios);
            }

            // 2. Бонус: Ищем просроченные академические тесты
            var expiredTests = await context.Set<UserTestProgress>()
                .Where(p => p.Status == AttemptStatus.InProgress
                         && p.TargetEndTime.HasValue
                         && p.TargetEndTime.Value <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredTests.Any())
            {
                foreach (var t in expiredTests)
                {
                    t.Status = AttemptStatus.Failed;
                    t.CompletedAt = DateTime.UtcNow;
                }
                context.Set<UserTestProgress>().UpdateRange(expiredTests);
            }

            // 3. Сохраняем и рассылаем сигналы
            if (expiredScenarios.Any() || expiredTests.Any())
            {
                await context.SaveChangesAsync();

                foreach (var p in expiredScenarios)
                {
                    if (p.IsTeamAttempt && p.TeamId.HasValue)
                        NotifyTeamUpdate(p.TeamId.Value);
                    else if (!string.IsNullOrEmpty(p.UserId))
                        NotifyUserUpdate(p.UserId);
                }

                foreach (var t in expiredTests)
                {
                    if (!string.IsNullOrEmpty(t.UserId))
                        NotifyUserUpdate(t.UserId);
                }

                NotifyAdminMonitor();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Фоновая задача] Ошибка проверки сессий: {ex.Message}");
        }
    }

    public void NotifyTeamUpdate(int teamId) => TeamProgressChanged?.Invoke(teamId);
    public void NotifyUserUpdate(string userId) => UserProgressChanged?.Invoke(userId);
    public void NotifyAdminMonitor() => ActiveSessionsChanged?.Invoke();

    public async Task<(bool Success, string ErrorMessage)> CheckConcurrencyAsync(string userId, int? targetTeamId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var activePersonal = await context.UserProgresses
            .AnyAsync(p => p.UserId == userId && p.Status == AttemptStatus.InProgress && !p.IsTeamAttempt);

        if (activePersonal)
            return (false, "У вас уже запущено одиночное расследование. Завершите его.");

        var userTeamIds = await context.Set<ApplicationUser>()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Teams.Select(t => t.Id))
            .ToListAsync();

        var activeTeamAttemptForUser = await context.UserProgresses
            .AnyAsync(p => p.IsTeamAttempt && p.Status == AttemptStatus.InProgress && userTeamIds.Contains(p.TeamId!.Value));

        if (activeTeamAttemptForUser)
            return (false, "Вы уже участвуете в активном командном сценарии в одной из ваших команд.");

        if (targetTeamId.HasValue)
        {
            var teamUserIds = await context.Set<UserTeam>()
                .Where(t => t.Id == targetTeamId.Value)
                .SelectMany(t => t.Users.Select(u => u.Id))
                .ToListAsync();

            foreach (var memberId in teamUserIds)
            {
                var memberBusyPersonal = await context.UserProgresses
                    .AnyAsync(p => p.UserId == memberId && p.Status == AttemptStatus.InProgress && !p.IsTeamAttempt);

                if (memberBusyPersonal)
                    return (false, $"Запуск невозможен. Один из участников команды (ID: {memberId}) проходит одиночный сценарий.");
            }
        }

        return (true, string.Empty);
    }

    public async Task ForceTerminateAsync(int progressId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var progress = await context.UserProgresses.FirstOrDefaultAsync(p => p.Id == progressId);
        if (progress != null && progress.Status == AttemptStatus.InProgress)
        {
            progress.Status = AttemptStatus.Failed;
            progress.CompletedAt = DateTime.UtcNow;
            progress.TimeSpent = DateTime.UtcNow - (progress.StartedAt ?? DateTime.UtcNow);
            await context.SaveChangesAsync();

            if (progress.IsTeamAttempt && progress.TeamId.HasValue)
                NotifyTeamUpdate(progress.TeamId.Value);
            else if (!string.IsNullOrEmpty(progress.UserId))
                NotifyUserUpdate(progress.UserId);

            NotifyAdminMonitor();
        }
    }

    public void Dispose()
    {
        _backgroundTimer?.Dispose();
    }
}