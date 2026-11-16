using System.Net.Http.Json;
using POS.Shared.DTOs;

namespace POS.Client.Services
{
    public class UserApiService : ApiClientBase
    {
        public UserApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<UserDto>> GetAllAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<UserDto>>("api/Users") ?? new List<UserDto>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(UserManagementRequest request)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync("api/Users", request);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(UserDto user, UserManagementRequest request)
        {
            SetAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/Users/{user.Id}", request);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> DeactivateAsync(int id)
        {
            SetAuthHeader();
            var response = await _http.DeleteAsync($"api/Users/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
