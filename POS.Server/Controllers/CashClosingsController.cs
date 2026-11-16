using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Services;
using POS.Shared.DTOs;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class CashClosingsController : ControllerBase
    {
        private readonly ICashClosingService _cashClosingService;
        public CashClosingsController(ICashClosingService cashClosingService)
        {
            _cashClosingService = cashClosingService;
        }
        [HttpGet]
        public async Task<ActionResult<List<POS.Shared.Entities.CashClosing>>> GetAll()
        {
            return Ok(await _cashClosingService.GetAllAsync());
        }
        [HttpGet("last")]
        public async Task<ActionResult<POS.Shared.Entities.CashClosing>> GetLast()
        {
            var last = await _cashClosingService.GetLastAsync();
            return Ok(last);
        }
        [HttpPost]
        public async Task<ActionResult<POS.Shared.Entities.CashClosing>> Create(CashClosingRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            request.UserId = userId;
            var (closing, error) = await _cashClosingService.CreateAsync(request);
            if (closing == null)
            {
                return BadRequest(error);
            }
            return Ok(closing);
        }
    }
}
