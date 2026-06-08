using Microsoft.AspNetCore.Identity;

namespace CyberPolygon.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // Пользователь принадлежит только одной группе
        public int? GroupId { get; set; }
        public UserGroup? Group { get; set; }

        // Пользователь может быть в нескольких командах
        public List<UserTeam> Teams { get; set; } = new();
    }

}
