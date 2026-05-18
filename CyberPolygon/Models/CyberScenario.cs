using System.ComponentModel.DataAnnotations;

public class CyberScenario
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Название обязательно")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание обязательно")]
    public string Description { get; set; } = string.Empty;

    public string Legend { get; set; } = string.Empty;

    public string Task { get; set; } = string.Empty;

    // Здесь будем хранить путь к файлу (например: /uploads/schemas/file.png)
    public string? SchemaPath { get; set; }
}