using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var databaseAvailable = await _context.Database.CanConnectAsync();
            return databaseAvailable
                ? Ok(new { status = "ok", database = "connected" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "error", database = "unavailable" });
        }
    }
}
