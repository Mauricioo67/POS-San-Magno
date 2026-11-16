using POS.Server.Repositories;
using POS.Shared.Entities;

namespace POS.Server.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        public CategoryService(ICategoryRepository categoryRepository, IProductRepository productRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }
        public Task<List<Category>> GetAllAsync()
        {
            return _categoryRepository.GetAllAsync();
        }
        public Task<Category?> GetByIdAsync(int id)
        {
            return _categoryRepository.GetByIdAsync(id);
        }
        public async Task<Category?> CreateAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return null;
            }
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim() ?? string.Empty;
            return await _categoryRepository.CreateAsync(category);
        }
        public async Task<bool> UpdateAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return false;
            }
            var existing = await _categoryRepository.GetByIdAsync(category.Id);
            if (existing == null)
            {
                return false;
            }
            existing.Name = category.Name.Trim();
            existing.Description = category.Description?.Trim() ?? string.Empty;
            await _categoryRepository.UpdateAsync(existing);
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return false;
            }
            var products = await _productRepository.GetAllAsync();
            if (products.Any(p => p.CategoryId == id))
            {
                return false; // Tiene productos asociados
            }
            await _categoryRepository.DeleteAsync(category);
            return true;
        }
    }
}
