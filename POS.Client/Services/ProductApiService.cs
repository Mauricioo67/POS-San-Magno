using System.Net.Http.Json;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class ProductApiService : ApiClientBase
    {
        public ProductApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Product>> GetAllAsync(bool onlyActive = false)
        {
            return await _http.GetFromJsonAsync<List<Product>>($"api/Products?onlyActive={onlyActive}") ?? new List<Product>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(Product product)
        {
            var response = await _http.PostAsJsonAsync("api/Products", product);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            return (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(Product product)
        {
            var response = await _http.PutAsJsonAsync($"api/Products/{product.Id}", product);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            return (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/Products/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
