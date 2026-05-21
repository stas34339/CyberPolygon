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
    AllAtOnce, // Все вопросы доступны сразу
    OneByOne   // Каждый следующий открывается после верного ответа
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

    // Игровой режим прохождения сценария
    public ScenarioGameMode GameMode { get; set; } = ScenarioGameMode.AllAtOnce;

    // Коллекция вопросов теста
    public List<ScenarioQuestion> Questions { get; set; } = new();
}

public class ScenarioQuestion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Текст вопроса обязателен")]
    public string Text { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ответ обязателен")]
    public string CorrectAnswer { get; set; } = string.Empty;

    public int AwardPoints { get; set; } = 10;   // Плюс баллы за верный
    public int PenaltyPoints { get; set; } = 5;  // Минус баллы за неверный

    public int CyberScenarioId { get; set; }     // Внешний ключ для EF Core
}

public class UserScenarioProgress
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public int CyberScenarioId { get; set; }
    public int Score { get; set; }

    // НОВЫЕ ПОЛЯ ВМЕСТО IsCompleted
    public AttemptStatus Status { get; set; } = AttemptStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? TargetEndTime { get; set; } // До какого времени нужно сдать
    public TimeSpan? TimeSpent { get; set; }     // Сколько реально потратил
    public DateTime? CompletedAt { get; set; }

    public CyberScenario Scenario { get; set; }
}

public class UserScenarioProgressDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Score { get; set; }

    public AttemptStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TimeSpent { get; set; }
}
