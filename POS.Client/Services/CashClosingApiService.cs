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
            return await _http.GetFromJsonAsync<List<CashClosing>>("api/CashClosings") ?? new List<CashClosing>();
        }

        public async Task<CashClosing?> GetLastAsync()
        {
            using var response = await _http.GetAsync("api/CashClosings/last");
            // Sin cierres registrados el servidor responde 204 sin cuerpo;
            // GetFromJsonAsync lanzaría JsonException en ese caso.
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CashClosing?>();
        }

        public async Task<(bool Success, CashClosing? Closing, string? Error)> CreateAsync(CashClosingRequest request)
        {
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
