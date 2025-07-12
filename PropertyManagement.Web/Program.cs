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