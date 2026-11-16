using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly AppDbContext _context;
        public StockRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Stock>> GetAllAsync(int? productId = null)
        {
            var query = _context.Stocks.Include(s => s.Product).Include(s => s.User).AsQueryable();
            if (productId.HasValue)
            {
                query = query.Where(s => s.ProductId == productId.Value);
            }
            return await query.OrderByDescending(s => s.MovementDate).ToListAsync();
        }
        public async Task<Stock> CreateAsync(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
            return stock;
        }
    }
}
