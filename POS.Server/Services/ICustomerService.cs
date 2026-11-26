using POS.Shared.Entities;

namespace POS.Server.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<(Customer? Customer, string? Error)> CreateAsync(Customer customer);
        Task<(bool Success, string? Error)> UpdateAsync(Customer customer);
        Task<(bool Success, string? Error)> DeactivateAsync(int id);
    }
}
