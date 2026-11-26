using System.Net.Http.Json;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class StockApiService : ApiClientBase
    {
        public StockApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Stock>> GetAllAsync(int? productId = null)
        {
            var url = productId.HasValue ? $"api/Stocks?productId={productId.Value}" : "api/Stocks";
            return await _http.GetFromJsonAsync<List<Stock>>(url) ?? new List<Stock>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(StockMovementRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/Stocks", request);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            return (false, await response.Content.ReadAsStringAsync());
        }
    }
}
