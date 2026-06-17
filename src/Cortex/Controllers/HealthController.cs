using Microsoft.AspNetCore.Mvc;
using Cortex.Database;

namespace Cortex.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly CortexDbContext _dbContext;

    public HealthController(CortexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Health check endpoint that validates database connectivity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool dbConnected = false;
        try
        {
            dbConnected = await _dbContext.Database.CanConnectAsync(ct);
        }
        catch
        {
            dbConnected = false;
        }

        return Ok(new
        {
            Status = dbConnected ? "Healthy" : "Degraded",
            DatabaseConnected = dbConnected,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Service = "Cortex.API"
        });
    }
}
