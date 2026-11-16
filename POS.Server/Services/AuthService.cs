using Microsoft.IdentityModel.Tokens;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace POS.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (user == null || !user.IsActive)
            {
                return null;
            }
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }
            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _userRepository.UserExistsAsync(email))
            {
                return null;
            }
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newUser = new User
            {
                Name = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                Role = POS.Shared.Enums.Role.Cajero,
                IsActive = true,
            };
            var createdUser = await _userRepository.CreateUserAsync(newUser);
            //Generamos inmediatamente el token o la logica requerida
            return GenerateAuthResponse(createdUser);
        }
        private AuthResponse GenerateAuthResponse(User user)
        {
            var tokenstring = GenerateToken(user);
            return new AuthResponse
            {
                Token = tokenstring,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.Name,
                Role = user.Role.ToString()
            };
        }
        private string GenerateToken(User user) 
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey no está configurada o es demasiado corta (mínimo 32 caracteres).");
            }
            var expirationMinutes = double.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 60;
            var key = Encoding.UTF8.GetBytes(secretKey);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var tonkenHandler = new JwtSecurityTokenHandler();
            var token = tonkenHandler.CreateToken(tokenDescriptor);
            return tonkenHandler.WriteToken(token);
        }
    }
}
