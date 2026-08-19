public class ScenarioDocument
{
    public int Id { get; set; }

    // Название документа, которое вводит пользователь (например, "План сети" или "Инструкция")
    public string Title { get; set; } = string.Empty;

    // Оригинальное имя загруженного файла (для скачивания)
    public string FileName { get; set; } = string.Empty;

    // MIME-тип файла
    public string ContentType { get; set; } = "application/octet-stream";

    // Содержимое файла хранится прямо в БД (Postgres: bytea)
    public byte[]? Content { get; set; }

    // Внешний ключ к сценарию
    public int CyberScenarioId { get; set; }
    public CyberScenario? CyberScenario { get; set; }
}