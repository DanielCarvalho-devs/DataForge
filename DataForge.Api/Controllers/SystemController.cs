using DataForge.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly DataForgeDbContext _context;

    public SystemController(DataForgeDbContext context)
    {
        _context = context;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var databaseConnected = await _context.Database.CanConnectAsync();

        if (!databaseConnected)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                api = "online",
                database = "offline"
            });
        }

        return Ok(new
        {
            status = "healthy",
            api = "online",
            database = "connected",
            application = "DataForge"
        });
    }
}