using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Services;
using Serilog;
using System.Globalization;
using System.IO;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Ensure log directory exists before Serilog is configured
Directory.CreateDirectory("/app/logs");

// Build configuration first
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from configuration (appsettings.json, environment variables, etc.)
builder.Host.UseSerilog((context, services, configuration) =>
{
  configuration
      .ReadFrom.Configuration(context.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext();
});

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

// Add OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(builder.Environment.ApplicationName))
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(options =>
            {
                // Read OTLP endpoint from environment variable, fallback to sensible default
                var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                    ?? (builder.Environment.IsDevelopment()
                        ? "http://localhost:4317"
                        : "http://otel-collector:4317");

                options.Endpoint = new Uri(otlpEndpoint);
            });
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

// Use Prometheus metrics endpoint
app.UseMetricServer(); // Exposes /metrics
app.UseHttpMetrics();  // Collects default HTTP metrics

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