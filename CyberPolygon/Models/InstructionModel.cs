using System;
using System.ComponentModel.DataAnnotations;

namespace CyberPolygon.Data.Models
{
    public class InstructionModel
    {
        public int Id { get; set; }

        public List<InstructionAttachment> Attachments { get; set; } = new();

        [Required(ErrorMessage = "Название инструкции обязательно для заполнения.")]
        [StringLength(150, ErrorMessage = "Название не может быть длиннее 150 символов.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Описание инструкции обязательно для заполнения.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Необходимо выбрать или указать иконку.")]
        public string IconName { get; set; } = "assignment";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class InstructionAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public int InstructionId { get; set; }
        public InstructionModel? Instruction { get; set; }
    }
}