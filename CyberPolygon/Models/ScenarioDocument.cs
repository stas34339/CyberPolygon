public class ScenarioDocument
{
    public int Id { get; set; }

    // Название документа, которое вводит пользователь (например, "План сети" или "Инструкция")
    public string Title { get; set; } = string.Empty;

    // Путь к сохраненному файлу на сервере
    public string FilePath { get; set; } = string.Empty;

    // Внешний ключ к сценарию
    public int CyberScenarioId { get; set; }
    public CyberScenario? CyberScenario { get; set; }
}