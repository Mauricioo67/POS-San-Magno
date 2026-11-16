using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface ISaleRepository
    {
        Task<List<Sale>> GetAllAsync();
        Task<Sale?> GetByIdAsync(int id);
        Task<int> AddSaleAsync(Sale sale);
        Task SaveChangesAsync();
    }
}
