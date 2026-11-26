using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<StockService> _logger;
        public StockService(IStockRepository stockRepository, IProductRepository productRepository, AppDbContext context, ILogger<StockService> logger)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
            _context = context;
            _logger = logger;
        }
        public Task<List<Stock>> GetMovementsAsync(int? productId = null)
        {
            return _stockRepository.GetAllAsync(productId);
        }
        public async Task<(bool Success, string? Error)> RegisterMovementAsync(StockMovementRequest request)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product == null)
            {
                return (false, "Producto no existe");
            }
            if (request.Quantity <= 0)
            {
                return (false, "La cantidad debe ser mayor a cero");
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int affected;
                switch (request.MovementType)
                {
                    case MovementType.Entrada:
                        affected = await _context.Product
                            .Where(p => p.Id == request.ProductId)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(p => p.CurrentStock, p => p.CurrentStock + request.Quantity));
                        break;
                    case MovementType.Salida:
                        affected = await _context.Product
                            .Where(p => p.Id == request.ProductId && p.CurrentStock >= request.Quantity)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(p => p.CurrentStock, p => p.CurrentStock - request.Quantity));
                        if (affected == 0)
                        {
                            await transaction.RollbackAsync();
                            return (false, $"Stock insuficiente (disponible: {product.CurrentStock})");
                        }
                        break;
                    case MovementType.Ajuste:
                        affected = await _context.Product
                            .Where(p => p.Id == request.ProductId)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(p => p.CurrentStock, request.Quantity));
                        if (affected == 0)
                        {
                            await transaction.RollbackAsync();
                            return (false, "Producto no existe");
                        }
                        break;
                    default:
                        await transaction.RollbackAsync();
                        return (false, "Tipo de movimiento inválido");
                }
                await _stockRepository.CreateAsync(new Stock
                {
                    ProductId = product.Id,
                    MovementDate = DateTime.Now,
                    MovementType = request.MovementType,
                    Quantity = request.Quantity,
                    Notes = request.Notes ?? string.Empty,
                    UserId = request.UserId
                });
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar el movimiento de stock para el producto {ProductId}", request.ProductId);
                return (false, "Error al registrar el movimiento de stock");
            }
        }
    }
}
