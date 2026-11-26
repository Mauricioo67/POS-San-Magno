using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public interface IDispatchService
    {
        Task<List<Dispatch>> GetAllAsync();
        Task<Dispatch?> GetByIdAsync(int id);
        Task<(Dispatch? Dispatch, string? Error)> CreateAsync(DispatchRequest request);
        Task<(bool Success, string? Error)> UpdateStatusAsync(int id, DispatchStatus status);
        Task<(bool Success, string? Error)> CancelAsync(int id, int userId);
    }
}
