using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Repositories
{
    public interface ISaleRepository
    {
        Task<List<Sale>> GetAllAsync();
        Task<Sale?> GetByIdAsync(int id);
        Task<Dictionary<PaymentMethod, decimal>> GetTotalsByPaymentMethodAsync(DateTime startDate, DateTime endDate);
        Task<int> AddSaleAsync(Sale sale);
        Task SaveChangesAsync();
    }
}
