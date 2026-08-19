using CyberPolygon.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public enum AttemptStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public enum ScenarioGameMode
{
    AllAtOnce,
    OneByOne
}

// Новое перечисление для области видимости
public enum VisibilityScope
{
    Public,       // Виден всем
    GroupOnly,    // Только определенной группе
    TeamOnly      // Только определенной команде
}

public class UserGroup
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Название группы обязательно")]
    public string Name { get; set; } = string.Empty;

    // Навигационные свойства
    public List<ApplicationUser> Users { get; set; } = new();
    public List<UserTeam> Teams { get; set; } = new();
}

public class UserTeam
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Название команды обязательно")]
    public string Name { get; set; } = string.Empty;

    public int GroupId { get; set; }
    public UserGroup Group { get; set; } = null!;

    // Многие-ко-многим: Пользователь может состоять в нескольких командах
    public List<ApplicationUser> Users { get; set; } = new();
}

public class CyberScenario
{
    public int Id { get; set; }
    public bool IsVisible { get; set; } = false;
    public List<ScenarioDocument> Documents { get; set; } = new();
    public string Task { get; set; } = string.Empty;

    [Required(ErrorMessage = "Название обязательно")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание обязательно")]
    public string Description { get; set; } = string.Empty;

    public string Legend { get; set; } = string.Empty;
    public string? SchemaPath { get; set; }
    public int DurationInMinutes { get; set; } = 60;
    public string? DocumentationPath { get; set; }
    public string? DocumentationFileName { get; set; }
    public ScenarioGameMode GameMode { get; set; } = ScenarioGameMode.AllAtOnce;
    public List<ScenarioQuestion> Questions { get; set; } = new();
    public List<ScenarioDevice> Devices { get; set; } = new List<ScenarioDevice>();
    public List<ScenarioConnection> Connections { get; set; } = new List<ScenarioConnection>();

    // НОВЫЕ ПОЛЯ ДОСТУПА
    public VisibilityScope Scope { get; set; } = VisibilityScope.Public;
    public int? TargetGroupId { get; set; }
    public int? TargetTeamId { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public string? AllowedUserIds { get; set; }
    public string? AllowedTeamIds { get; set; }
    public string? AuthorId { get; set; }


}

public class ScenarioQuestion
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Текст вопроса обязателен")]
    public string Text { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ответ обязателен")]
    public string CorrectAnswer { get; set; } = string.Empty;
    public int AwardPoints { get; set; } = 10;
    public int PenaltyPoints { get; set; } = 5;
    public int CyberScenarioId { get; set; }
    public string ExampleAnswer { get; set; } = string.Empty;
}

public class UserScenarioProgress
{
    public int Id { get; set; }

    // Поля стали Nullable, так как сессия теперь либо юзера, либо команды
    public string? UserId { get; set; }
    public int? TeamId { get; set; }
    public bool IsTeamAttempt { get; set; } // Флаг командного прохождения

    public int CyberScenarioId { get; set; }
    public int Score { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? TargetEndTime { get; set; }
    public TimeSpan? TimeSpent { get; set; }
    public DateTime? CompletedAt { get; set; }

    public CyberScenario Scenario { get; set; } = null!;

    public UserTeam? Team { get; set; }
    public bool IsRetakeRequested { get; set; } = false;
    public bool IsRetakeGranted { get; set; } = false;
}

public class UserScenarioProgressDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // ДОБАВЛЕННЫЕ ПОЛЯ ДЛЯ КОМАНД:
    public bool IsTeamAttempt { get; set; }
    public int? TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;

    public int Score { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TimeSpent { get; set; }
}

// Заглушка, если у тебя используется этот класс
