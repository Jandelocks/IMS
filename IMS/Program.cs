using IMS.Data;
using IMS.Repositories;
using IMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using reCAPTCHA.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;

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
    // options.Filters.Add(new AuthorizeFilter(policy));
});

// Add Distributed Cache (required for Session)
builder.Services.AddDistributedMemoryCache();

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

// --- HANGFIRE: use builder.Configuration (was 'configuration' which is undefined) ---
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"))
);

// Add Hangfire Server
builder.Services.AddHangfireServer();

// Logging & Session Services
builder.Services.AddScoped<LogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionService>();

// SignalR (Real-time Notifications)
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();

// Repositories & Services registrations...
builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IModeratorRepository, ModeratorRepository>();
builder.Services.AddScoped<IModeratorService, ModeratorService>();

//Session
builder.Services.AddScoped<ISingleSessionManagerService, SingleSessionManagerService>();

//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromHours(1);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

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
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseMiddleware<UserValidationMiddleware>();
app.UseAuthorization();

// Hangfire Dashboard (optional — you can protect it with authorization)
app.UseHangfireDashboard("/hangfire"); // Consider adding Authorization if needed

// Serve static files (for wwwroot or other folders)
app.UseDefaultFiles();  // optional – serves index.html if present
app.UseStaticFiles();   // enable static file serving

// Map MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Homepage}/{action=Index}/{id?}");


// SignalR Hub Mapping
app.MapHub<IncidentHub>("/incidentHub");
app.MapHub<NotificationHub>("/notificationHub");

// Example: schedule a recurring job (every 5 minutes)
RecurringJob.AddOrUpdate("my-job-id", () => Console.WriteLine("Hello from Hangfire!"), "*/5 * * * *");

app.Run();
