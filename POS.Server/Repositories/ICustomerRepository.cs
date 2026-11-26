using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByDocumentAsync(string documentNumber);
        Task<bool> DocumentExistsAsync(string documentNumber, int? exceptCustomerId = null);
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
    }
}
