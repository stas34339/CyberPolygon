namespace CyberPolygon.Models
{
    public class DeviceApplication
    {
        public int Id { get; set; }
        public int ScenarioDeviceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Vulnerability { get; set; } = string.Empty; // Если нужно хранить CVE или описание уязвимости
        public string Description { get; set; } = string.Empty;
    }
}
