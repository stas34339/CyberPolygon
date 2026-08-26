using CyberPolygon.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;

namespace CyberPolygon.Services
{
    public class UserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuthenticationStateProvider _authStateProvider;

        public UserManagementService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AuthenticationStateProvider authStateProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authStateProvider = authStateProvider;
        }

        public async Task<(bool Success, string Error)> CreateUserAsync(string login, string password, string firstName, string lastName, string middleName, bool isAdmin)
        {
            string emailLogin = login.Contains("@") ? login : $"{login}@polygon.do";
            var user = new ApplicationUser
            {
                UserName = login,
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
            };

            // ПРОВЕРКА ПРАВ: Только SuperAdmin может назначать других Админов
            if (isAdmin)
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (!authState.User.IsInRole("SuperAdmin"))
                    return (false, "ОТКАЗ СИСТЕМЫ: Только СуперАдминистратор может назначать привилегированные права.");
            }

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

            if (isAdmin)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));

                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return (true, string.Empty);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ToggleAdminRoleAsync(string userId)
        {
            // ПРОВЕРКА ПРАВ: Снимать или выдавать админку на лету может только SuperAdmin
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (!authState.User.IsInRole("SuperAdmin"))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");

            return true;
        }

        public async Task<bool> IsUserAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Считаем администратором и Admin и SuperAdmin
            return await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users
                .Include(u => u.Group)
                .Include(u => u.Teams)
                .ToListAsync();
        }
    }
}