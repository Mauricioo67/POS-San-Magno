using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public class SaleService : ISaleService
    {
        private readonly AppDbContext _context;
        private readonly ISaleRepository _saleRepository;
        private readonly ILogger<SaleService> _logger;
        public SaleService(AppDbContext context, ISaleRepository saleRepository, ILogger<SaleService> logger)
        {
            _context = context;
            _saleRepository = saleRepository;
            _logger = logger;
        }
        public Task<List<Sale>> GetAllAsync()
        {
            return _saleRepository.GetAllAsync();
        }
        public Task<Sale?> GetByIdAsync(int id)
        {
            return _saleRepository.GetByIdAsync(id);
        }
        public async Task<(Sale? Sale, string? Error)> CreateSaleAsync(SaleRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return (null, "La venta no tiene productos");
            }
            if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
            {
                return (null, "Método de pago inválido");
            }
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Product
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);
            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    return (null, $"Cantidad inválida para el producto {item.ProductId}");
                }
            }
            var quantitiesByProduct = request.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            foreach (var (productId, quantity) in quantitiesByProduct)
            {
                if (!products.TryGetValue(productId, out var product))
                {
                    return (null, $"Producto {productId} no existe");
                }
                if (!product.IsActive)
                {
                    return (null, $"El producto {product.Name} está inactivo");
                }
                if (product.CurrentStock < quantity)
                {
                    return (null, $"Stock insuficiente para {product.Name} (disponible: {product.CurrentStock})");
                }
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    UserId = request.UserId,
                    PaymentMethod = request.PaymentMethod,
                    SaleDetails = new List<SaleDetail>()
                };
                decimal total = 0;
                foreach (var item in request.Items.GroupBy(i => i.ProductId).Select(g => new SaleItemDto { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) }))
                {
                    var product = products[item.ProductId];
                    var detail = new SaleDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * item.Quantity
                    };
                    sale.SaleDetails.Add(detail);
                    total += detail.TotalPrice;
                    var affected = await _context.Product
                        .Where(p => p.Id == item.ProductId && p.IsActive && p.CurrentStock >= item.Quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.CurrentStock, p => p.CurrentStock - item.Quantity));
                    if (affected == 0)
                    {
                        await transaction.RollbackAsync();
                        return (null, $"Stock insuficiente para {product.Name}");
                    }
                    _context.Stocks.Add(new Stock
                    {
                        ProductId = product.Id,
                        MovementDate = sale.SaleDate,
                        MovementType = MovementType.Salida,
                        Quantity = item.Quantity,
                        Notes = $"Venta",
                        UserId = request.UserId
                    });
                }
                sale.TotalAmount = total;
                await _saleRepository.AddSaleAsync(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (sale, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar la venta para el usuario {UserId}", request.UserId);
                return (null, "Error al registrar la venta");
            }
        }
    }
}
