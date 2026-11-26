using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Entities;

namespace POS.Server.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;
        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetByDocumentAsync(string documentNumber)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.DocumentNumber == documentNumber);
        }

        public async Task<bool> DocumentExistsAsync(string documentNumber, int? exceptCustomerId = null)
        {
            return await _context.Customers.AnyAsync(c => c.DocumentNumber == documentNumber
                && (!exceptCustomerId.HasValue || c.Id != exceptCustomerId.Value));
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
    }
}
