using IMS.Data;
using IMS.Repositories;
using IMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using reCAPTCHA.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1. Add services to the container
// ==============================

// Add Controllers with Authorization Policy
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    //options.Filters.Add(new AuthorizeFilter(policy));
});

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
    options.AddPolicy("User", policy => policy.RequireRole("user"));
    options.AddPolicy("Moderator", policy => policy.RequireRole("moderator"));
});

// reCAPTCHA
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("GoogleReCaptcha"));
builder.Services.AddTransient<IRecaptchaService, RecaptchaService>();

// Reports Service
builder.Services.AddScoped<ReportService>();

// Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Logging & Session Services
builder.Services.AddScoped<LogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionService>();

// SignalR (Real-time Notifications)
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();


builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IModeratorRepository, ModeratorRepository>();
builder.Services.AddScoped<IModeratorService, ModeratorService>();
// Add Distributed Cache (Required for Session)
builder.Services.AddDistributedMemoryCache();

// ==============================
// 2. Build the app
// ==============================
var app = builder.Build();

// run seeder on startup (only if tables are empty)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    // Will apply migrations and seed only when table(s) are empty
    Seeders.RunSeeders(db);
}
// ==============================
// 3. Configure the HTTP Request Pipeline
// ==============================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware Order (IMPORTANT!)
app.UseHttpsRedirection();
app.UseStaticFiles();  // Serve static files (CSS, JS, etc.)
app.UseRouting();
app.UseSession(); // Ensure session is enabled before authentication
app.UseAuthentication();
app.UseMiddleware<UserValidationMiddleware>();
app.UseAuthorization();


// Map static assets and routes
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Homepage}/{action=Index}/{id?}"
).WithStaticAssets();

// SignalR Hub Mapping
app.MapHub<IncidentHub>("/incidentHub");
app.MapHub<NotificationHub>("/notificationHub");

// Run the application
app.Run();
