using Serilog;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using PropertyManagement.Domain.Entities;
using Microsoft.AspNetCore.Diagnostics;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("/app/logs/propertymanagement.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "propertymanagement-logs-{0:yyyy.MM.dd}"
    })
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Require authentication globally (force login for all pages except [AllowAnonymous])
builder.Services.AddAuthorization(options =>
{
  options.FallbackPolicy = new AuthorizationPolicyBuilder()
      .RequireAuthenticatedUser()
      .Build();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(0)
    ));
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISmsService, BulkSmsService>();
builder.Services.AddHostedService<RentReminderService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.LoginPath = "/Tenants/Login";
      options.LogoutPath = "/Tenants/Logout";
      options.AccessDeniedPath = "/Tenants/AccessDenied";
      // Change the cookie name on every app start to force logout for all users
      options.Cookie.Name = "PropertyManagementAuth";
    });

builder.WebHost.ConfigureKestrel(options =>
{
    if (!builder.Environment.IsDevelopment())
    {       
        options.ListenAnyIP(80); // HTTP for production
        options.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps("https/aspnetapp.pfx", "YourPassword123");
        });
    }
});

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
  var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  context.Database.Migrate();

  // Read the setting from configuration
  var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
  var enableSeeding = config.GetValue<bool>("EnableDatabaseSeeding");

  if (enableSeeding)
  {
    DatabaseSeeder.Seed(context);
  }
    // Check if a manager already exists
    if (!context.Users.Any(u => u.Role == "Manager"))
    {
      var manager = new User
      {
        Username = "Admin",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("01Pa$$w0rd2025#"),
        Role = "Manager"
      };
      context.Users.Add(manager);
      context.SaveChanges();
    }
  
}

// Set default culture to South Africa
var defaultCulture = new CultureInfo("en-ZA");
var localizationOptions = new RequestLocalizationOptions
{
  DefaultRequestCulture = new RequestCulture(defaultCulture),
  SupportedCultures = new List<CultureInfo> { defaultCulture },
  SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exceptionHandlerPathFeature?.Error, "Unhandled exception occurred");
            context.Response.Redirect("/Home/Error");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Test log from PropertyManagement.Web startup");

app.Run();