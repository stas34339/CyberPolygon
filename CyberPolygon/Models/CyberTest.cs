using System.ComponentModel.DataAnnotations;
using CyberPolygon.Data; // Твои общие Enum (VisibilityScope, AttemptStatus)

public enum TestDifficulty { Easy, Medium, Hard }
public enum QuestionType { ManualText, SingleChoice, MultipleChoice }

public class CyberTest
{

    public int Id { get; set; }
    public bool IsVisible { get; set; }
    public VisibilityScope Scope { get; set; } = VisibilityScope.Public;
    public int? TargetGroupId { get; set; }
    public int? TargetTeamId { get; set; }

    [Required] public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public TestDifficulty Difficulty { get; set; } = TestDifficulty.Easy;
    public int DurationInMinutes { get; set; } = 30;

    // Внутри класса CyberTest
    public List<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    public List<TestDocument> Documents { get; set; } = new();

    public string? Tags { get; set; } = string.Empty; // Если нужны теги
    public string? AllowedUserIds { get; set; }
    public string? AllowedTeamIds { get; set; }
    public string? AuthorId { get; set; }
}

public class TestQuestion
{
    public int Id { get; set; }
    public int CyberTestId { get; set; }
    [Required] public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.ManualText;
    public string? CorrectTextAnswer { get; set; } // Для ручного ввода
    public List<TestOption> Options { get; set; } = new();
    public string? ExampleAnswer { get; set; }
}

public class TestOption
{
    public int Id { get; set; }
    public int TestQuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class TestDocument
{
    public int Id { get; set; }
    public int CyberTestId { get; set; }
    public string Title { get; set; } = string.Empty;

    // Оригинальное имя загруженного файла (для скачивания)
    public string FileName { get; set; } = string.Empty;

    // MIME-тип файла
    public string ContentType { get; set; } = "application/octet-stream";

    // Содержимое файла хранится прямо в БД (Postgres: bytea)
    public byte[]? Content { get; set; }
}

public class UserTestProgress
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CyberTestId { get; set; }
    public int Score { get; set; } // Кол-во правильных ответов
    public AttemptStatus Status { get; set; } = AttemptStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? TargetEndTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsRetakeRequested { get; set; } = false;
    public bool IsRetakeGranted { get; set; } = false;

    public CyberTest Test { get; set; } = null!;
}

public class UserTestAnswer
{
    public int Id { get; set; }
    public int UserTestProgressId { get; set; }
    public int QuestionId { get; set; }
    public bool IsCorrect { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty; // Сохраненный ответ пользователя
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow; // Время ответа
}