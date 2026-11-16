using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface IStockRepository
    {
        Task<List<Stock>> GetAllAsync(int? productId = null);
        Task<Stock> CreateAsync(Stock stock);
    }
}
