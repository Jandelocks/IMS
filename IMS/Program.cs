using IMS.Data;
using IMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using reCAPTCHA.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

// Add authentication using cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
    options.AddPolicy("User", policy => policy.RequireRole("user"));
    options.AddPolicy("Moderator", policy => policy.RequireRole("moderator"));
});

// Add reCAPTCHA service
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("GoogleReCaptcha"));
builder.Services.AddTransient<IRecaptchaService, RecaptchaService>();

//Reports
builder.Services.AddScoped<ReportService>();

// Add SQL Server database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register LogService
builder.Services.AddScoped<LogService>();

// Register SessionService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache(); // Required for session storage

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting(); 

app.UseAuthentication();
app.UseAuthorization();  

app.UseSession(); 

app.MapStaticAssets();

app.MapControllerRoute(
    //name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}")
    //.WithStaticAssets();

    name: "default",
    pattern: "{controller=Homepage}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
