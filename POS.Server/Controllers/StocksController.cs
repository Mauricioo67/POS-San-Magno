using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Services;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;
        public StocksController(IStockService stockService)
        {
            _stockService = stockService;
        }
        [HttpGet]
        public async Task<ActionResult<List<Stock>>> GetAll([FromQuery] int? productId = null)
        {
            return Ok(await _stockService.GetMovementsAsync(productId));
        }
        [HttpPost]
        public async Task<IActionResult> Create(StockMovementRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (request.MovementType == MovementType.Ajuste && role != Role.Administrador.ToString())
            {
                return Forbid();
            }
            request.UserId = userId;
            var (success, error) = await _stockService.RegisterMovementAsync(request);
            if (!success)
            {
                return BadRequest(error);
            }
            return NoContent();
        }
    }
}
