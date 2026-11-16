using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Services
{
    public interface ISaleService
    {
        Task<List<Sale>> GetAllAsync();
        Task<Sale?> GetByIdAsync(int id);
        Task<(Sale? Sale, string? Error)> CreateSaleAsync(SaleRequest request);
    }
}
