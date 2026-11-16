using System.Net.Http.Json;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class CashClosingApiService : ApiClientBase
    {
        public CashClosingApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<CashClosing>> GetAllAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<CashClosing>>("api/CashClosings") ?? new List<CashClosing>();
        }

        public async Task<CashClosing?> GetLastAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<CashClosing?>("api/CashClosings/last");
        }

        public async Task<(bool Success, CashClosing? Closing, string? Error)> CreateAsync(CashClosingRequest request)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync("api/CashClosings", request);
            if (response.IsSuccessStatusCode)
            {
                var closing = await response.Content.ReadFromJsonAsync<CashClosing>();
                return (true, closing, null);
            }
            return (false, null, await response.Content.ReadAsStringAsync());
        }
    }
}
