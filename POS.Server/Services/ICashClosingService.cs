using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Services
{
    public interface ICashClosingService
    {
        Task<List<CashClosing>> GetAllAsync();
        Task<CashClosing?> GetLastAsync();
        Task<(CashClosing? Closing, string? Error)> CreateAsync(CashClosingRequest request);
    }
}
