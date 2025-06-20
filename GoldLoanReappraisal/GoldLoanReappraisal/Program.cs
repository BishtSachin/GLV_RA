using GoldLoanReappraisal.Components;
using GoldLoanReappraisal.Data.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using Serilog.Debugging;
using System.Diagnostics;

// --- SERILOG CONFIGURATION ---
SelfLog.Enable(msg => Debug.WriteLine(msg));
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // --- SERVICE REGISTRATION ---
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/";
            options.LogoutPath = "/api/auth/logout";
            options.AccessDeniedPath = "/error/403";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;

            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

    builder.Services.AddAuthorization();
    builder.Services.AddHttpClient();
    builder.Services.AddControllers();
    builder.Services.AddScoped<CaptchaService>();
    builder.Services.AddScoped<UserValidationService>();
    builder.Services.AddScoped<UserProfileService>();
    builder.Services.AddScoped<UserManagementService>();
    builder.Services.AddSingleton<NavigationMenuService>();

    var app = builder.Build();

    // --- HTTP REQUEST PIPELINE (Correct and Final Order) ---
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    // ADD THIS LINE to handle HTTP status code errors like 404
    app.UseStatusCodePagesWithReExecute("/error/{0}");

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // 1. UseRouting must come first.
    app.UseRouting();

    // 2. Auth middleware comes next.
    app.UseAuthentication();
    app.UseAuthorization();

    // 3. Antiforgery comes after auth.
    app.UseAntiforgery();

    // 4. Finally, map the endpoints.
    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(GoldLoanReappraisal.Client._Imports).Assembly);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}