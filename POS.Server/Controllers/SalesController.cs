using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Services;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }
        [HttpGet]
        public async Task<ActionResult<List<Sale>>> GetAll()
        {
            return Ok(await _saleService.GetAllAsync());
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Sale>> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return Ok(sale);
        }
        [HttpPost]
        public async Task<ActionResult<Sale>> Create(SaleRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            request.UserId = userId;
            var (sale, error) = await _saleService.CreateSaleAsync(request);
            if (sale == null)
            {
                return BadRequest(error);
            }
            return Ok(sale);
        }
    }
}
