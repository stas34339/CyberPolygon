using System.ComponentModel.DataAnnotations;

namespace CyberPolygon.Data
{
    public class ScenarioConnection
    {
        [Key]
        public int Id { get; set; }

        public int CyberScenarioId { get; set; }
        public CyberScenario? CyberScenario { get; set; }

        // Какое устройство соединяем (Откуда)
        public int FromDeviceId { get; set; }
        public ScenarioDevice? FromDevice { get; set; }

        // С каким устройством соединяем (Куда)
        public int ToDeviceId { get; set; }
        public ScenarioDevice? ToDevice { get; set; }

        // Дополнительно: тип линка (например: "Ethernet", "Trunk", "VPN")
        public string LinkType { get; set; } = "Ethernet";
    }
}