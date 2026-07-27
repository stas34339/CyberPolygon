namespace CyberPolygon.Services
{
    public class CatalogUpdateService
    {
        public event Action? OnCatalogChanged;
        public void NotifyCatalogChanged() => OnCatalogChanged?.Invoke();
    }
}