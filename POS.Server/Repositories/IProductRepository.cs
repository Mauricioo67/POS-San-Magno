using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync(bool onlyActive = false);
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByCodeAsync(string code);
        Task<bool> CodeExistsAsync(string code);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
