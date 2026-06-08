using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CyberPolygon.Data;
using Microsoft.EntityFrameworkCore;

public class ScenarioSessionManager
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    // Событие для Real-time обновления страниц воркспейса у команд
    public event Action<int>? TeamProgressChanged; // Параметр: TeamId
    // Событие для обновления админ-панели (списка живых сессий)
    public event Action? ActiveSessionsChanged;

    public ScenarioSessionManager(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void NotifyTeamUpdate(int teamId) => TeamProgressChanged?.Invoke(teamId);
    public void NotifyAdminMonitor() => ActiveSessionsChanged?.Invoke();

    /// <summary>
    /// Проверка: Может ли пользователь начать прохождение (одиночное или за команду)
    /// </summary>
    public async Task<(bool Success, string ErrorMessage)> CheckConcurrencyAsync(string userId, int? targetTeamId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // 1. Ищем, нет ли у самого юзера незавершенной ЛИЧНОЙ сессии
        var activePersonal = await context.UserProgresses
            .AnyAsync(p => p.UserId == userId && p.Status == AttemptStatus.InProgress && !p.IsTeamAttempt);

        if (activePersonal)
            return (false, "У вас уже запущено одиночное расследование. Завершите его.");

        // 2. Ищем, не проходит ли этот юзер прямо сейчас сценарий в составе КАКОЙ-ЛИБО из своих команд
        var userTeamIds = await context.Set<ApplicationUser>()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Teams.Select(t => t.Id))
            .ToListAsync();

        var activeTeamAttemptForUser = await context.UserProgresses
            .AnyAsync(p => p.IsTeamAttempt && p.Status == AttemptStatus.InProgress && userTeamIds.Contains(p.TeamId!.Value));

        if (activeTeamAttemptForUser)
            return (false, "Вы уже участвуете в активном командном сценарии в одной из ваших команд.");

        // 3. Если запуск планируется для Команды, проверяем, чтобы НИ ОДИН член команды не был занят
        if (targetTeamId.HasValue)
        {
            var teamUserIds = await context.Set<UserTeam>()
                .Where(t => t.Id == targetTeamId.Value)
                .SelectMany(t => t.Users.Select(u => u.Id))
                .ToListAsync();

            foreach (var memberId in teamUserIds)
            {
                // Проверяем личную занятость каждого
                var memberBusyPersonal = await context.UserProgresses
                    .AnyAsync(p => p.UserId == memberId && p.Status == AttemptStatus.InProgress && !p.IsTeamAttempt);

                if (memberBusyPersonal)
                    return (false, $"Запуск невозможен. Один из участников команды (ID: {memberId}) проходит одиночный сценарий.");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Принудительное завершение сессии администратором
    /// </summary>
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
            {
                NotifyTeamUpdate(progress.TeamId.Value);
            }
            NotifyAdminMonitor();
        }
    }


}