using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public interface IDispatchRepository
    {
        Task<List<Dispatch>> GetAllAsync();
        Task<Dispatch?> GetByIdAsync(int id);
        Task<Dispatch> AddAsync(Dispatch dispatch);
        Task SaveChangesAsync();
    }
}
