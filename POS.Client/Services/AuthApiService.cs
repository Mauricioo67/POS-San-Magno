using System.Net.Http.Json;
using System.Text.Json;
using POS.Shared.DTOs;

namespace POS.Client.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _http;
        private readonly AuthStateService _auth;

        public AuthApiService(HttpClient http, AuthStateService auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Auth/Login", request);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Credenciales inválidas"
                        : $"Error del servidor al iniciar sesión ({(int)response.StatusCode})");
                }

                var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (auth == null)
                {
                    return (false, "Respuesta inválida del servidor");
                }

                await _auth.SetUserAsync(auth);
                return (true, null);
            }
            catch (HttpRequestException)
            {
                return (false, "No se pudo conectar con el servidor. Verifica que POS.Server esté iniciado y que ApiBaseUrl apunte a su dirección.");
            }
            catch (TaskCanceledException)
            {
                return (false, "La solicitud de inicio de sesión agotó el tiempo de espera.");
            }
            catch (JsonException)
            {
                return (false, "El servidor devolvió una respuesta inválida al iniciar sesión.");
            }
        }
    }
}
