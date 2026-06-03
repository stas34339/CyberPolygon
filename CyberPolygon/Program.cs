using CyberPolygon.Components;
using CyberPolygon.Components.Account;
using CyberPolygon.Data;
using CyberPolygon.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);


// Сервис для работы с бд
builder.Services.AddScoped<ScenarioService>();
//Подключение сервиса Radzen
builder.Services.AddRazorPages();
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddSingleton<ScenarioSessionManager>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<InstructionService>();
builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "ApplicationTheme";
    options.Duration = TimeSpan.FromDays(365);
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
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
// 2. Регистрируем обычный Scoped контекст, который берется из этой фабрики (для ASP.NET Core Identity)
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext()); 

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        // Твои настройки паролей, если нужны
    })
    .AddRoles<IdentityRole>() // ВКЛЮЧАЕМ РОЛИ
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();


// АВТОМАТИЧЕСКОЕ СОЗДАНИЕ ПОЛЬЗОВАТЕЛЯ ПРИ СТАРТЕ

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Ищем по email, так надежнее
    var existingUser = await userManager.FindByEmailAsync("user@cyberpolygon.ru");
    if (existingUser == null)
    {
        var newUser = new ApplicationUser
        {
            UserName = "user1@cyberpolygon.RU", // Делаем UserName таким же как Email
            Email = "user1@cyberpolygon.RU",
            EmailConfirmed = true,
            NormalizedUserName = "USER1@CYBERPOLYGON.RU", // Принудительно заполняем регистр
            NormalizedEmail = "USER1@CYBERPOLYGON.RU"
        };

        // Создаем пользователя с простым паролем
        var result = await userManager.CreateAsync(newUser, "user1@cyberpolygon.RU");

        if (result.Succeeded)
        {
            Console.WriteLine("====== [УСПЕХ] Тестовый пользователь 'user@cyberpolygon.ru' с паролем 'user123' создан! ======");
        }
        else
        {
            Console.WriteLine("====== [ОШИБКА] Не удалось создать пользователя: ======");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
}



app.Run();
