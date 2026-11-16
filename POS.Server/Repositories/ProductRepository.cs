using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Product>> GetAllAsync(bool onlyActive = false)
        {
            var query = _context.Product.Include(p => p.Category).AsQueryable();
            if (onlyActive)
            {
                query = query.Where(p => p.IsActive);
            }
            return await query.OrderBy(p => p.Name).ToListAsync();
        }
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Product.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task<Product?> GetByCodeAsync(string code)
        {
            return await _context.Product.Include(p => p.Category).FirstOrDefaultAsync(p => p.Code == code);
        }
        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.Product.AnyAsync(p => p.Code == code);
        }
        public async Task<Product> CreateAsync(Product product)
        {
            _context.Product.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }
        public async Task UpdateAsync(Product product)
        {
            _context.Product.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Product.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            product.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
