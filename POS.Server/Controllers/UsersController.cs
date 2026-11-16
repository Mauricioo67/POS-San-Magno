using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(users.Select(ToDto).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(UserManagementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrEmpty(request.Password) || request.Password.Length < 6 || !Enum.IsDefined(request.Role))
            {
                return BadRequest("Nombre, email, rol válido y contraseña de al menos 6 caracteres son obligatorios.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (await _userRepository.UserExistsAsync(email))
            {
                return Conflict("Ya existe un usuario con ese email.");
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                IsActive = request.IsActive
            };
            await _userRepository.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetAll), new { id = user.Id }, ToDto(user));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserDto>> Update(int id, UserManagementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email)
                || (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 6)
                || !Enum.IsDefined(request.Role))
            {
                return BadRequest("Nombre, email y rol válido son obligatorios; la nueva contraseña debe tener al menos 6 caracteres.");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (await _userRepository.UserExistsAsync(email, id))
            {
                return Conflict("Ya existe un usuario con ese email.");
            }

            if (user.Id == GetCurrentUserId() && !request.IsActive)
            {
                return BadRequest("No puedes desactivar tu propia cuenta.");
            }

            if (user.IsActive && user.Role == Role.Administrador
                && (!request.IsActive || request.Role != Role.Administrador)
                && (await _userRepository.GetAllAsync()).Count(u => u.IsActive && u.Role == Role.Administrador) <= 1)
            {
                return BadRequest("No se puede desactivar ni cambiar el rol del último administrador activo.");
            }

            user.Name = request.Name.Trim();
            user.Email = email;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            await _userRepository.UpdateAsync(user);
            return Ok(ToDto(user));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            if (user.Id == GetCurrentUserId())
            {
                return BadRequest("No puedes desactivar tu propia cuenta.");
            }
            if (user.IsActive && user.Role == Role.Administrador
                && (await _userRepository.GetAllAsync()).Count(u => u.IsActive && u.Role == Role.Administrador) <= 1)
            {
                return BadRequest("No se puede desactivar el último administrador activo.");
            }

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        private static UserDto ToDto(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive
        };
    }
}
