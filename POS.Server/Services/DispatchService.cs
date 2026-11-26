using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public class DispatchService : IDispatchService
    {
        private readonly AppDbContext _context;
        private readonly IDispatchRepository _dispatchRepository;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(AppDbContext context, IDispatchRepository dispatchRepository, ILogger<DispatchService> logger)
        {
            _context = context;
            _dispatchRepository = dispatchRepository;
            _logger = logger;
        }

        public Task<List<Dispatch>> GetAllAsync()
        {
            return _dispatchRepository.GetAllAsync();
        }

        public Task<Dispatch?> GetByIdAsync(int id)
        {
            return _dispatchRepository.GetByIdAsync(id);
        }

        public async Task<(Dispatch? Dispatch, string? Error)> CreateAsync(DispatchRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return (null, "El despacho no tiene productos");
            }

            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId);
            if (customer == null)
            {
                return (null, "El cliente no existe");
            }
            if (!customer.IsActive)
            {
                return (null, "El cliente está inactivo");
            }

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

            var productIds = quantitiesByProduct.Keys.ToList();
            var products = await _context.Product
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

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
                var dispatch = new Dispatch
                {
                    DispatchDate = DateTime.Now,
                    CustomerId = customer.Id,
                    UserId = request.UserId,
                    Status = DispatchStatus.Pendiente,
                    Notes = request.Notes?.Trim() ?? string.Empty,
                    DispatchDetails = new List<DispatchDetail>()
                };

                decimal total = 0;
                foreach (var (productId, quantity) in quantitiesByProduct)
                {
                    var product = products[productId];
                    var detail = new DispatchDetail
                    {
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * quantity
                    };
                    dispatch.DispatchDetails.Add(detail);
                    total += detail.TotalPrice;

                    var affected = await _context.Product
                        .Where(p => p.Id == productId && p.IsActive && p.CurrentStock >= quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.CurrentStock, p => p.CurrentStock - quantity));
                    if (affected == 0)
                    {
                        await transaction.RollbackAsync();
                        return (null, $"Stock insuficiente para {product.Name}");
                    }

                    _context.Stocks.Add(new Stock
                    {
                        ProductId = product.Id,
                        MovementDate = dispatch.DispatchDate,
                        MovementType = MovementType.Salida,
                        Quantity = quantity,
                        Notes = "Salida por despacho",
                        UserId = request.UserId
                    });
                }

                dispatch.TotalAmount = total;
                await _dispatchRepository.AddAsync(dispatch);
                await _context.SaveChangesAsync();

                dispatch.DispatchNumber = $"DSP-{dispatch.Id:D6}";
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (dispatch, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar el despacho para el cliente {CustomerId}", request.CustomerId);
                return (null, "Error al registrar el despacho");
            }
        }

        public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, DispatchStatus status)
        {
            if (!Enum.IsDefined(typeof(DispatchStatus), status))
            {
                return (false, "Estado de despacho inválido");
            }
            if (status == DispatchStatus.Cancelado)
            {
                return (false, "Para cancelar un despacho usa la operación de cancelación");
            }

            var dispatch = await _context.Dispatches
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dispatch == null)
            {
                return (false, "El despacho no existe");
            }
            if (dispatch.Status == DispatchStatus.Cancelado)
            {
                return (false, "Un despacho cancelado no puede cambiar de estado");
            }
            if (status <= dispatch.Status)
            {
                return (false, "Solo se permite avanzar el estado del despacho");
            }

            var affected = await _context.Dispatches
                .Where(d => d.Id == id && d.Status != DispatchStatus.Cancelado && d.Status < status)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.Status, status));
            if (affected == 0)
            {
                return (false, "El estado del despacho cambió mientras se procesaba la solicitud. Intenta de nuevo.");
            }
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> CancelAsync(int id, int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dispatch = await _context.Dispatches
                    .AsNoTracking()
                    .Include(d => d.DispatchDetails)
                    .FirstOrDefaultAsync(d => d.Id == id);
                if (dispatch == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "El despacho no existe");
                }
                if (dispatch.Status == DispatchStatus.Cancelado)
                {
                    await transaction.RollbackAsync();
                    return (false, "El despacho ya está cancelado");
                }
                if (dispatch.Status == DispatchStatus.Entregado)
                {
                    await transaction.RollbackAsync();
                    return (false, "No se puede cancelar un despacho entregado");
                }

                var affected = await _context.Dispatches
                    .Where(d => d.Id == id && d.Status != DispatchStatus.Cancelado && d.Status != DispatchStatus.Entregado)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(d => d.Status, DispatchStatus.Cancelado));
                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "El despacho cambió de estado mientras se procesaba la solicitud. Intenta de nuevo.");
                }

                foreach (var detail in dispatch.DispatchDetails)
                {
                    await _context.Product
                        .Where(p => p.Id == detail.ProductId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.CurrentStock, p => p.CurrentStock + detail.Quantity));

                    _context.Stocks.Add(new Stock
                    {
                        ProductId = detail.ProductId,
                        MovementDate = DateTime.Now,
                        MovementType = MovementType.Entrada,
                        Quantity = detail.Quantity,
                        Notes = $"Cancelación de despacho {dispatch.DispatchNumber}",
                        UserId = userId
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cancelar el despacho {DispatchId}", id);
                return (false, "Error al cancelar el despacho");
            }
        }
    }
}
