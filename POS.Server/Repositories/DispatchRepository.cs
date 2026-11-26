using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class DispatchRepository : IDispatchRepository
    {
        private readonly AppDbContext _context;
        public DispatchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dispatch>> GetAllAsync()
        {
            return await _context.Dispatches
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.User)
                .Include(d => d.DispatchDetails).ThenInclude(d => d.Product)
                .OrderByDescending(d => d.DispatchDate)
                .ToListAsync();
        }

        public async Task<Dispatch?> GetByIdAsync(int id)
        {
            return await _context.Dispatches
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.User)
                .Include(d => d.DispatchDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public Task<Dispatch> AddAsync(Dispatch dispatch)
        {
            _context.Dispatches.Add(dispatch);
            return Task.FromResult(dispatch);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
