using System.Net.Http.Json;
using POS.Shared.Entities;

namespace POS.Client.Services
{
    public class CategoryApiService : ApiClientBase
    {
        public CategoryApiService(HttpClient http, AuthStateService auth) : base(http, auth) { }

        public async Task<List<Category>> GetAllAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<Category>>("api/Categories") ?? new List<Category>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(Category category)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync("api/Categories", category);
            var (success, _, error) = await ReadAsync<Category>(response);
            return (success, error);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(Category category)
        {
            SetAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/Categories/{category.Id}", category);
            return response.IsSuccessStatusCode ? (true, null) : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            SetAuthHeader();
            var response = await _http.DeleteAsync($"api/Categories/{id}");
            return response.IsSuccessStatusCode ? (true, null) : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
