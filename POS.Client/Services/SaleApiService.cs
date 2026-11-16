using System.Net.Http.Json;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class SaleApiService : ApiClientBase
    {
        public SaleApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Sale>> GetAllAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<Sale>>("api/Sales") ?? new List<Sale>();
        }

        public async Task<(bool Success, Sale? Sale, string? Error)> CreateAsync(SaleRequest request)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync("api/Sales", request);
            if (response.IsSuccessStatusCode)
            {
                var sale = await response.Content.ReadFromJsonAsync<Sale>();
                return (true, sale, null);
            }
            return (false, null, await response.Content.ReadAsStringAsync());
        }
    }
}
