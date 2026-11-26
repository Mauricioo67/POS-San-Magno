using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Repositories;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly AppDbContext _context;

        public CustomerService(ICustomerRepository customerRepository, AppDbContext context)
        {
            _customerRepository = customerRepository;
            _context = context;
        }

        public Task<List<Customer>> GetAllAsync()
        {
            return _customerRepository.GetAllAsync();
        }

        public Task<Customer?> GetByIdAsync(int id)
        {
            return _customerRepository.GetByIdAsync(id);
        }

        public async Task<(Customer? Customer, string? Error)> CreateAsync(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.DocumentNumber))
            {
                return (null, "El nombre y el documento del cliente son obligatorios");
            }

            var document = customer.DocumentNumber.Trim();
            if (await _customerRepository.DocumentExistsAsync(document))
            {
                return (null, "Ya existe un cliente con ese documento");
            }

            customer.Name = customer.Name.Trim();
            customer.DocumentNumber = document;
            customer.Email = customer.Email?.Trim() ?? string.Empty;
            customer.Phone = customer.Phone?.Trim() ?? string.Empty;
            customer.Address = customer.Address?.Trim() ?? string.Empty;
            customer.IsActive = true;
            customer.CreatedAt = DateTime.Now;

            var created = await _customerRepository.CreateAsync(customer);
            return (created, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(Customer customer)
        {
            var existing = await _customerRepository.GetByIdAsync(customer.Id);
            if (existing == null)
            {
                return (false, "El cliente no existe");
            }

            if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.DocumentNumber))
            {
                return (false, "El nombre y el documento del cliente son obligatorios");
            }

            var document = customer.DocumentNumber.Trim();
            if (await _customerRepository.DocumentExistsAsync(document, existing.Id))
            {
                return (false, "Ya existe un cliente con ese documento");
            }

            existing.Name = customer.Name.Trim();
            existing.DocumentNumber = document;
            existing.Email = customer.Email?.Trim() ?? string.Empty;
            existing.Phone = customer.Phone?.Trim() ?? string.Empty;
            existing.Address = customer.Address?.Trim() ?? string.Empty;
            existing.IsActive = customer.IsActive;

            await _customerRepository.UpdateAsync(existing);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeactivateAsync(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return (false, "El cliente no existe");
            }

            var hasOpenDispatches = await _context.Dispatches.AnyAsync(d => d.CustomerId == id
                && (d.Status == DispatchStatus.Pendiente || d.Status == DispatchStatus.Enviado));
            if (hasOpenDispatches)
            {
                return (false, "No se puede desactivar un cliente con despachos pendientes o enviados");
            }

            customer.IsActive = false;
            await _customerRepository.UpdateAsync(customer);
            return (true, null);
        }
    }
}
