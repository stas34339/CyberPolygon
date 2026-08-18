using CyberPolygon.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace CyberPolygon.Components.Pages
{
    public partial class Home : IAsyncDisposable
    {
        private bool isLoaded;
        private string UserDisplayName = "Оперативник";

        // Переменная для хранения уровня.
        private int UserLevel = 1;

        private ApplicationUser? dbUser;
        private bool globeInitialized;

        // 1 = Сценарии, 2 = Задания
        private int selectedMode = 1;

        [Inject] private IJSRuntime JS { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthProvider.GetAuthenticationStateAsync();
                var userClaims = authState.User;

                if (userClaims.Identity?.IsAuthenticated != true)
                {
                    UserDisplayName = "Гость полигона";
                    return;
                }

                var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                await using var context = await ContextFactory.CreateDbContextAsync();

                dbUser = await context.Set<ApplicationUser>()
                    .Include(u => u.Group)
                    .Include(u => u.Teams)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (dbUser != null)
                {
                    var parts = new[] { dbUser.LastName, dbUser.FirstName }
                        .Where(x => !string.IsNullOrWhiteSpace(x));

                    UserDisplayName = parts.Any()
                        ? string.Join(" ", parts)
                        : userClaims.Identity?.Name ?? "Оперативник";

                    // ОШИБКА CS1061 УСТРАНЕНА ЗДЕСЬ:
                    // Если вы добавили public int Level { get; set; } в ApplicationUser 
                    // и применили миграцию EF Core, раскомментируйте строчку ниже:
                    // UserLevel = dbUser.Level; 

                    // А пока поля в БД нет, оставляем значение по умолчанию, чтобы проект компилировался:
                    UserLevel = 1;
                }
                else
                {
                    UserDisplayName = userClaims.Identity?.Name ?? "Оперативник";
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки инициализации
                Console.WriteLine($"Ошибка инициализации данных пользователя: {ex.Message}");
                UserDisplayName = "Оперативник";
            }
            finally
            {
                isLoaded = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !globeInitialized)
            {
                try
                {
                    await Task.Delay(60);
                    // Инициализация готового кода Three.js для планеты
                    await JS.InvokeVoidAsync("CyberGlobe.init", "cyber-globe");
                    globeInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки CyberGlobe: {ex.Message}");
                }
            }
        }

        private void SelectMode(int mode) => selectedMode = mode;

        private void GoToSelected()
        {
            if (selectedMode == 1)
                NavigationManager.NavigateTo("/scenario");
            else
                NavigationManager.NavigateTo("/tests");
        }

        // Dispose паттерн для корректной очистки ресурсов Three.js
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (globeInitialized)
                {
                    await JS.InvokeVoidAsync("CyberGlobe.destroy", "cyber-globe");
                }
            }
            catch (JSDisconnectedException)
            {
                // Игнорируем ошибку при отключении SignalR
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении CyberGlobe: {ex.Message}");
            }
        }
    }
}