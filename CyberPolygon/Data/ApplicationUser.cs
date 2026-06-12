using Microsoft.AspNetCore.Identity;

namespace CyberPolygon.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int? GroupId { get; set; }
        public UserGroup? Group { get; set; }
        public List<UserTeam> Teams { get; set; } = new();

        // Новые поля
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        // Вычисляемое свойство для полного имени
        public string FullName => $"{LastName} {FirstName}".Trim();
    }

}
