using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MigrationController> _logger;
        private readonly IConfiguration _configuration;

        public MigrationController(
            ApplicationDbContext context, 
            ILogger<MigrationController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyMigrations([FromHeader] string? authToken)
        {
            try
            {
                // Simple security check using FTP password as auth token
                var expectedToken = _configuration["FTP_PASSWORD"] ?? _configuration["MigrationAuthToken"];
                if (string.IsNullOrEmpty(authToken) || authToken != expectedToken)
                {
                    _logger.LogWarning("Unauthorized migration attempt from {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized("Invalid authorization token");
                }

                _logger.LogInformation("Starting automated migration process...");

                // Check if database can be reached
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database");
                    return StatusCode(500, "Database connection failed");
                }

                // Get pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                
                if (!pendingMigrations.Any())
                {
                    _logger.LogInformation("No pending migrations found");
                    return Ok(new { 
                        success = true, 
                        message = "Database is up to date - no pending migrations",
                        pendingMigrations = 0
                    });
                }

                _logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));

                // Apply migrations
                await _context.Database.MigrateAsync();

                _logger.LogInformation("? Database migrations applied successfully!");

                return Ok(new { 
                    success = true, 
                    message = "Database migrations applied successfully",
                    appliedMigrations = pendingMigrations.Count(),
                    migrations = pendingMigrations.ToList()
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to apply database migrations");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Migration failed",
                    error = ex.Message 
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetMigrationStatus()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return Ok(new { 
                        connected = false, 
                        message = "Cannot connect to database" 
                    });
                }

                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                return Ok(new {
                    connected = true,
                    appliedMigrations = appliedMigrations.Count(),
                    pendingMigrations = pendingMigrations.Count(),
                    lastAppliedMigration = appliedMigrations.LastOrDefault(),
                    nextPendingMigration = pendingMigrations.FirstOrDefault(),
                    isUpToDate = !pendingMigrations.Any()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration status");
                return StatusCode(500, new { 
                    connected = false, 
                    error = ex.Message 
                });
            }
        }
    }
}