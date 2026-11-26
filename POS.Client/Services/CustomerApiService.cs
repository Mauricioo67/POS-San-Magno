using System.Net.Http.Json;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class CustomerApiService : ApiClientBase
    {
        public CustomerApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<List<Customer>>("api/Customers") ?? new List<Customer>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(Customer customer)
        {
            var response = await _http.PostAsJsonAsync("api/Customers", customer);
            var (success, _, error) = await ReadAsync<Customer>(response);
            return (success, error);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(Customer customer)
        {
            var response = await _http.PutAsJsonAsync($"api/Customers/{customer.Id}", customer);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> DeactivateAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/Customers/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
