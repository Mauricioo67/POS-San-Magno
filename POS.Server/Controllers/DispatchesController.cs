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
    public class DispatchesController : ControllerBase
    {
        private readonly IDispatchService _dispatchService;
        public DispatchesController(IDispatchService dispatchService)
        {
            _dispatchService = dispatchService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Dispatch>>> GetAll()
        {
            return Ok(await _dispatchService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Dispatch>> GetById(int id)
        {
            var dispatch = await _dispatchService.GetByIdAsync(id);
            if (dispatch == null)
            {
                return NotFound();
            }
            return Ok(dispatch);
        }

        [HttpPost]
        public async Task<ActionResult<Dispatch>> Create(DispatchRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            request.UserId = userId;

            var (dispatch, error) = await _dispatchService.CreateAsync(request);
            if (dispatch == null)
            {
                return BadRequest(error);
            }
            return Ok(dispatch);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, DispatchStatusUpdateRequest request)
        {
            var (success, error) = await _dispatchService.UpdateStatusAsync(id, request.Status);
            if (!success)
            {
                return BadRequest(error);
            }
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var (success, error) = await _dispatchService.CancelAsync(id, userId);
            if (!success)
            {
                return BadRequest(error);
            }
            return NoContent();
        }
    }
}
