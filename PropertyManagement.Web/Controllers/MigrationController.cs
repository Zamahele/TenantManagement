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
                // Multiple auth token sources for flexibility
                var expectedTokens = new[]
                {
                    _configuration["FTP_PASSWORD"],
                    _configuration["MigrationAuthToken"],
                    Environment.GetEnvironmentVariable("FTP_PASSWORD"),
                    Environment.GetEnvironmentVariable("MIGRATION_AUTH_TOKEN")
                }.Where(t => !string.IsNullOrEmpty(t)).ToArray();

                if (string.IsNullOrEmpty(authToken) || !expectedTokens.Any(token => authToken == token))
                {
                    _logger.LogWarning("Unauthorized migration attempt from {IP} with token {Token}", 
                        HttpContext.Connection.RemoteIpAddress, 
                        string.IsNullOrEmpty(authToken) ? "null" : "***");
                    return Unauthorized(new { 
                        success = false,
                        message = "Invalid or missing authorization token",
                        hint = "Provide authToken header with valid credentials"
                    });
                }

                _logger.LogInformation("Starting automated migration process from {IP}...", HttpContext.Connection.RemoteIpAddress);

                // Check if database can be reached
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database during migration attempt");
                    return StatusCode(500, new { 
                        success = false,
                        message = "Database connection failed",
                        details = "Unable to establish connection to the database"
                    });
                }

                // Get pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                
                if (!pendingMigrations.Any())
                {
                    _logger.LogInformation("No pending migrations found - database is up to date");
                    return Ok(new { 
                        success = true, 
                        message = "Database is up to date - no pending migrations",
                        pendingMigrations = 0,
                        appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).Count()
                    });
                }

                _logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));

                // Apply migrations
                await _context.Database.MigrateAsync();

                _logger.LogInformation("? Database migrations applied successfully!");

                // Verify migrations were applied
                var remainingPending = await _context.Database.GetPendingMigrationsAsync();
                var totalApplied = (await _context.Database.GetAppliedMigrationsAsync()).Count();

                return Ok(new { 
                    success = true, 
                    message = "Database migrations applied successfully",
                    appliedMigrations = pendingMigrations.Count(),
                    totalMigrations = totalApplied,
                    remainingPending = remainingPending.Count(),
                    migrationsApplied = pendingMigrations.ToList()
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to apply database migrations");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Migration failed",
                    error = ex.Message,
                    type = ex.GetType().Name
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
                        message = "Cannot connect to database",
                        timestamp = DateTime.UtcNow
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
                    isUpToDate = !pendingMigrations.Any(),
                    timestamp = DateTime.UtcNow,
                    databaseProvider = _context.Database.ProviderName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration status");
                return StatusCode(500, new { 
                    connected = false, 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return Ok(new {
                    status = canConnect ? "healthy" : "unhealthy",
                    database = canConnect ? "connected" : "disconnected",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}