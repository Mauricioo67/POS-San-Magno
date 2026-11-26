using POS.Shared.DTOs;

namespace POS.Server.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
    }
}
