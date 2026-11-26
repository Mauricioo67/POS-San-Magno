using System.Net.Http.Json;

namespace POS.Client.Services
{
    public abstract class ApiClientBase
    {
        protected readonly HttpClient _http;
        protected readonly AuthStateService _auth;

        protected ApiClientBase(HttpClient http, AuthStateService auth)
        {
            _http = http;
            _auth = auth;
        }

        protected async Task<(bool Success, T? Result, string? Error)> ReadAsync<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                return (true, result, null);
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, default, string.IsNullOrWhiteSpace(error) ? $"Error HTTP {(int)response.StatusCode}" : error);
        }
    }
}
