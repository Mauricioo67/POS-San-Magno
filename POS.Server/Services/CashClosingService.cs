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
        public CashClosingService(ICashClosingRepository cashClosingRepository, ISaleRepository saleRepository)
        {
            _cashClosingRepository = cashClosingRepository;
            _saleRepository = saleRepository;
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
            var last = await _cashClosingRepository.GetLastAsync();
            var startDate = last?.EndDate ?? new DateTime(2000, 1, 1);
            var endDate = DateTime.Now;
            var sales = await _saleRepository.GetAllAsync();
            var periodSales = sales.Where(s => s.SaleDate > startDate && s.SaleDate <= endDate).ToList();
            var totalCash = periodSales.Where(s => s.PaymentMethod == PaymentMethod.Efectivo).Sum(s => s.TotalAmount);
            var totalCard = periodSales.Where(s => s.PaymentMethod == PaymentMethod.Tarjeta).Sum(s => s.TotalAmount);
            var totalTransfer = periodSales.Where(s => s.PaymentMethod == PaymentMethod.Transferencia).Sum(s => s.TotalAmount);
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
            return (created, null);
        }
    }
}
