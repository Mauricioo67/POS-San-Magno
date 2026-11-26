using System.Net.Http.Json;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Client.Services
{
    public class DispatchApiService : ApiClientBase
    {
        public DispatchApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Dispatch>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<List<Dispatch>>("api/Dispatches") ?? new List<Dispatch>();
        }

        public async Task<(bool Success, Dispatch? Dispatch, string? Error)> CreateAsync(DispatchRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/Dispatches", request);
            if (response.IsSuccessStatusCode)
            {
                var dispatch = await response.Content.ReadFromJsonAsync<Dispatch>();
                return (true, dispatch, null);
            }
            return (false, null, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, DispatchStatus status)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/Dispatches/{id}/status",
                new DispatchStatusUpdateRequest { Status = status });
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> CancelAsync(int id)
        {
            var response = await _http.PostAsync($"api/Dispatches/{id}/cancel", null);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
