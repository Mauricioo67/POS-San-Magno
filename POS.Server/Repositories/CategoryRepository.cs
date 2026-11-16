using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Category.OrderBy(c => c.Name).ToListAsync();
        }
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Category.FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Category.AnyAsync(c => c.Id == id);
        }
        public async Task<Category> CreateAsync(Category category)
        {
            _context.Category.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }
        public async Task UpdateAsync(Category category)
        {
            _context.Category.Update(category);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Category category)
        {
            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
