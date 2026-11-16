using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Services
{
    public interface IStockService
    {
        Task<List<Stock>> GetMovementsAsync(int? productId = null);
        Task<(bool Success, string? Error)> RegisterMovementAsync(StockMovementRequest request);
    }
}
