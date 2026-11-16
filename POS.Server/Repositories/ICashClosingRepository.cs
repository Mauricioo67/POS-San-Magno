using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface ICashClosingRepository
    {
        Task<List<CashClosing>> GetAllAsync();
        Task<CashClosing?> GetByIdAsync(int id);
        Task<CashClosing?> GetLastAsync();
        Task<CashClosing> CreateAsync(CashClosing cashClosing);
    }
}
