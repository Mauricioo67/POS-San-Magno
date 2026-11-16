using POS.Server.Repositories;
using POS.Shared.Entities;

namespace POS.Server.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }
        public Task<List<Product>> GetAllAsync(bool onlyActive = false)
        {
            return _productRepository.GetAllAsync(onlyActive);
        }
        public Task<Product?> GetByIdAsync(int id)
        {
            return _productRepository.GetByIdAsync(id);
        }
        public Task<Product?> GetByCodeAsync(string code)
        {
            return _productRepository.GetByCodeAsync(code);
        }
        public async Task<Product?> CreateAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Code) || string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
            {
                return null;
            }
            if (!await _categoryRepository.ExistsAsync(product.CategoryId))
            {
                return null; // Categoría inexistente
            }
            product.Code = product.Code.Trim().ToUpper();
            if (await _productRepository.CodeExistsAsync(product.Code))
            {
                return null; // Código duplicado
            }
            product.Name = product.Name.Trim();
            product.IsActive = true;
            return await _productRepository.CreateAsync(product);
        }
        public async Task<bool> UpdateAsync(Product product)
        {
            var existing = await _productRepository.GetByIdAsync(product.Id);
            if (existing == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(product.Code) || string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
            {
                return false;
            }
            if (!await _categoryRepository.ExistsAsync(product.CategoryId))
            {
                return false; // Categoría inexistente
            }
            var normalizedCode = product.Code.Trim().ToUpper();
            if (existing.Code != normalizedCode)
            {
                var other = await _productRepository.GetByCodeAsync(normalizedCode);
                if (other != null && other.Id != existing.Id)
                {
                    return false; // Código duplicado
                }
                existing.Code = normalizedCode;
            }
            existing.Name = product.Name.Trim();
            existing.Description = product.Description?.Trim() ?? string.Empty;
            existing.Price = product.Price;
            existing.StockMinimun = product.StockMinimun;
            existing.CategoryId = product.CategoryId;
            existing.IsActive = product.IsActive;
            existing.ImageUrl = product.ImageUrl?.Trim() ?? string.Empty;
            await _productRepository.UpdateAsync(existing);
            return true;
        }

        public Task<bool> DeleteAsync(int id)
        {
            return _productRepository.DeleteAsync(id);
        }
    }
}
