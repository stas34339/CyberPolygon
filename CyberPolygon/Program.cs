using CyberPolygon.Components;
using CyberPolygon.Components.Account;
using CyberPolygon.Data;
using CyberPolygon.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using static CyberPolygon.Data.ApplicationDbContext;

var builder = WebApplication.CreateBuilder(args);

// Сервисы для работы с БД и логикой
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddRazorPages();

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();

builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<InstructionService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddSingleton<CatalogUpdateService>();
builder.Services.AddSingleton<ScenarioSessionManager>();

builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "ApplicationTheme";
    options.Duration = TimeSpan.FromDays(365);
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Увеличиваем лимит сообщений SignalR до 50 МБ
        options.MaximumReceiveMessageSize = 50 * 1024 * 1024;
    })
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();


builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Регистрируем обычный Scoped контекст для ASP.NET Core Identity
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddErrorDescriber<RussianIdentityErrorDescriber>()
    .AddRoles<IdentityRole>() // ВКЛЮЧАЕМ РОЛИ
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();


// ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ И РОЛЕЙ
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // 1. ПРОВЕРКА И СОЗДАНИЕ РОЛЕЙ
    string adminRoleName = "Admin";
    string superAdminRoleName = "SuperAdmin"; // НОВАЯ РОЛЬ

    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        Console.WriteLine($"====== [УСПЕХ] Роль '{adminRoleName}' успешно создана! ======");
    }

    if (!await roleManager.RoleExistsAsync(superAdminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(superAdminRoleName));
        Console.WriteLine($"====== [УСПЕХ] Роль '{superAdminRoleName}' успешно создана! ======");
    }

    // 2. СОЗДАНИЕ ПЕРВОГО СУПЕРАДМИНИСТРАТОРА (stas34339)
    string adminEmail1 = "stas34339@gmail.COM";
    var existingAdmin1 = await userManager.FindByEmailAsync(adminEmail1);

    if (existingAdmin1 == null)
    {
        var newAdmin1 = new ApplicationUser
        {
            UserName = adminEmail1,
            Email = adminEmail1,
            EmailConfirmed = true
        };

        var adminResult1 = await userManager.CreateAsync(newAdmin1, adminEmail1);

        if (adminResult1.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin1, superAdminRoleName);
            Console.WriteLine($"====== [УСПЕХ] Администратор '{adminEmail1}' создан и получил роль '{superAdminRoleName}'! ======");
        }
        else
        {
            Console.WriteLine($"====== [ОШИБКА] Не удалось создать администратора '{adminEmail1}': ======");
            foreach (var error in adminResult1.Errors) Console.WriteLine($"- {error.Description}");
        }
    }
    else
    {
        // Гарантируем, что существующий профиль имеет права SuperAdmin
        if (!await userManager.IsInRoleAsync(existingAdmin1, superAdminRoleName))
        {
            await userManager.AddToRoleAsync(existingAdmin1, superAdminRoleName);
            Console.WriteLine($"====== [УСПЕХ] Права профиля '{adminEmail1}' повышены до '{superAdminRoleName}'! ======");
        }
    }
}

app.Run();