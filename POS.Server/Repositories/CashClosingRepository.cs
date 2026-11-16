using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class CashClosingRepository : ICashClosingRepository
    {
        private readonly AppDbContext _context;
        public CashClosingRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<CashClosing>> GetAllAsync()
        {
            return await _context.CashClosing.Include(c => c.User).OrderByDescending(c => c.EndDate).ToListAsync();
        }
        public async Task<CashClosing?> GetByIdAsync(int id)
        {
            return await _context.CashClosing.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<CashClosing?> GetLastAsync()
        {
            return await _context.CashClosing.Include(c => c.User).OrderByDescending(c => c.EndDate).FirstOrDefaultAsync();
        }
        public async Task<CashClosing> CreateAsync(CashClosing cashClosing)
        {
            _context.CashClosing.Add(cashClosing);
            await _context.SaveChangesAsync();
            return cashClosing;
        }
    }
}
