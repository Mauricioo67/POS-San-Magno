using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _context;
        public SaleRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Sale>> GetAllAsync()
        {
            return await _context.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.SaleDetails).ThenInclude(d => d.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }
        public async Task<Sale?> GetByIdAsync(int id)
        {
            return await _context.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.SaleDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        public async Task<Dictionary<PaymentMethod, decimal>> GetTotalsByPaymentMethodAsync(DateTime startDate, DateTime endDate)
        {
            var totals = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate > startDate && s.SaleDate <= endDate)
                .GroupBy(s => s.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(s => s.TotalAmount) })
                .ToListAsync();
            return totals.ToDictionary(t => t.Method, t => t.Total);
        }
        public Task<int> AddSaleAsync(Sale sale)
        {
            _context.Sales.Add(sale);
            return Task.FromResult(sale.Id);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
