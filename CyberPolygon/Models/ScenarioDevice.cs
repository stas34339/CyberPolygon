using CyberPolygon.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberPolygon.Data // Убедись, что namespace совпадает с твоим проектом
{
    public class ScenarioDevice
    {
        [Key]
        public int Id { get; set; }

        // Внешний ключ для связи со сценарием
        public int CyberScenarioId { get; set; }
        public CyberScenario? CyberScenario { get; set; }

        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public string OpenPorts { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompromised { get; set; }

        // Координаты на интерактивной карте
        public int MapX { get; set; }
        public int MapY { get; set; }

        public string Type { get; set; } = "dns"; // По умолчанию — сервер

        public List<DeviceApplication> Applications { get; set; } = new();
    }
}