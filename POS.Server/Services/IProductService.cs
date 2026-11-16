using POS.Shared.Entities;

namespace POS.Server.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllAsync(bool onlyActive = false);
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByCodeAsync(string code);
        Task<Product?> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
