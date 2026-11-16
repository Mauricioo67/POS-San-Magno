using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task UpdateAsync(User user);
        Task<bool> UserExistsAsync(string email, int? exceptUserId = null);
    }
}
