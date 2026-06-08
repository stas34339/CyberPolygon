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


using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // 1. ПРОВЕРКА И СОЗДАНИЕ РОЛИ АДМИНА
    string adminRoleName = "Admin";
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        if (roleResult.Succeeded)
        {
            Console.WriteLine($"====== [УСПЕХ] Роль '{adminRoleName}' успешно создана в БД! ======");
        }
    }

    // 2. СОЗДАНИЕ ПЕРВОГО АДМИНИСТРАТОРА (stas34339)
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
            await userManager.AddToRoleAsync(newAdmin1, adminRoleName);
            Console.WriteLine($"====== [УСПЕХ] Администратор '{adminEmail1}' создан и получил роль '{adminRoleName}'! ======");
        }
        else
        {
            Console.WriteLine($"====== [ОШИБКА] Не удалось создать администратора '{adminEmail1}': ======");
            foreach (var error in adminResult1.Errors) Console.WriteLine($"- {error.Description}");
        }
    }

    // 3. СОЗДАНИЕ ВТОРОГО АДМИНИСТРАТОРА (skakalinma)
    string adminEmail2 = "skakalinma1@gmail.COM";
    var existingAdmin2 = await userManager.FindByEmailAsync(adminEmail2);
    if (existingAdmin2 == null)
    {
        var newAdmin2 = new ApplicationUser
        {
            UserName = adminEmail2,
            Email = adminEmail2,
            EmailConfirmed = true
        };

        var adminResult2 = await userManager.CreateAsync(newAdmin2, adminEmail2);

        if (adminResult2.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin2, adminRoleName);
            Console.WriteLine($"====== [УСПЕХ] Администратор '{adminEmail2}' создан и получил роль '{adminRoleName}'! ======");
        }
        else
        {
            Console.WriteLine($"====== [ОШИБКА] Не удалось создать администратора '{adminEmail2}': ======");
            foreach (var error in adminResult2.Errors) Console.WriteLine($"- {error.Description}");
        }
    }

    // 4. СОЗДАНИЕ ОБЫЧНОГО ПОЛЬЗОВАТЕЛЯ
    string userEmail = "user@gmail.COM";
    var existingUser = await userManager.FindByEmailAsync(userEmail);
    if (existingUser == null)
    {
        var newUser = new ApplicationUser
        {
            UserName = userEmail,
            Email = userEmail,
            EmailConfirmed = true
        };

        var userResult = await userManager.CreateAsync(newUser, userEmail);

        if (userResult.Succeeded)
        {
            Console.WriteLine($"====== [УСПЕХ] Обычный пользователь '{userEmail}' успешно создан! ======");
        }
        else
        {
            Console.WriteLine($"====== [ОШИБКА] Не удалось создать пользователя '{userEmail}': ======");
            foreach (var error in userResult.Errors) Console.WriteLine($"- {error.Description}");
        }
    }
}



app.Run();
