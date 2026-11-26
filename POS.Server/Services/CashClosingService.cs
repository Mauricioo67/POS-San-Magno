using System.Data;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public class CashClosingService : ICashClosingService
    {
        private readonly ICashClosingRepository _cashClosingRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<CashClosingService> _logger;
        public CashClosingService(
            ICashClosingRepository cashClosingRepository,
            ISaleRepository saleRepository,
            AppDbContext context,
            ILogger<CashClosingService> logger)
        {
            _cashClosingRepository = cashClosingRepository;
            _saleRepository = saleRepository;
            _context = context;
            _logger = logger;
        }
        public Task<List<CashClosing>> GetAllAsync()
        {
            return _cashClosingRepository.GetAllAsync();
        }
        public Task<CashClosing?> GetLastAsync()
        {
            return _cashClosingRepository.GetLastAsync();
        }
        public async Task<(CashClosing? Closing, string? Error)> CreateAsync(CashClosingRequest request)
        {
            if (request.InitialBalance < 0)
            {
                return (null, "El saldo inicial no puede ser negativo");
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var last = await _cashClosingRepository.GetLastAsync();
                var startDate = last?.EndDate ?? new DateTime(2000, 1, 1);
                var endDate = DateTime.Now;
                if (endDate <= startDate)
                {
                    await transaction.RollbackAsync();
                    return (null, "Ya existe un cierre de caja que cubre el período actual");
                }

                var totals = await _saleRepository.GetTotalsByPaymentMethodAsync(startDate, endDate);
                var totalCash = totals.GetValueOrDefault(PaymentMethod.Efectivo);
                var totalCard = totals.GetValueOrDefault(PaymentMethod.Tarjeta);
                var totalTransfer = totals.GetValueOrDefault(PaymentMethod.Transferencia);

                var closing = new CashClosing
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    InitialBalance = request.InitialBalance,
                    TotalCash = totalCash,
                    TotalCard = totalCard,
                    TotalTransfer = totalTransfer,
                    TotalAdjustments = request.TotalAdjustments,
                    FinalCash = request.InitialBalance + totalCash + request.TotalAdjustments,
                    FinalBalance = request.InitialBalance + totalCash + totalCard + totalTransfer + request.TotalAdjustments,
                    UserId = request.UserId,
                    Notes = request.Notes ?? string.Empty
                };
                var created = await _cashClosingRepository.CreateAsync(closing);
                await transaction.CommitAsync();
                return (created, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar el cierre de caja para el usuario {UserId}", request.UserId);
                return (null, "No se pudo registrar el cierre de caja. Intenta de nuevo.");
            }
        }
    }
}
