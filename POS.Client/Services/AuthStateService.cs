using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace POS.Client.Services
{
    public class AuthStateService
    {
        private readonly IJSRuntime _js;
        private bool _loaded;

        public string? Token { get; private set; }
        public int UserId { get; private set; }
        public string? Email { get; private set; }
        public string? FullName { get; private set; }
        public string? Role { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => Role == "Administrador";

        public event Action? OnChange;

        public AuthStateService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitializeAsync()
        {
            if (_loaded)
            {
                return;
            }
            try
            {
                Token = await _js.InvokeAsync<string?>("localStorage.getItem", "pos_token");
                if (!string.IsNullOrEmpty(Token) && IsTokenExpired(Token))
                {
                    await LogoutAsync();
                    _loaded = true;
                    return;
                }
                var userIdStr = await _js.InvokeAsync<string?>("localStorage.getItem", "pos_userId");
                int.TryParse(userIdStr, out var id);
                UserId = id;
                Email = await _js.InvokeAsync<string?>("localStorage.getItem", "pos_email");
                FullName = await _js.InvokeAsync<string?>("localStorage.getItem", "pos_fullname");
                Role = await _js.InvokeAsync<string?>("localStorage.getItem", "pos_role");
            }
            catch
            {
                // JS interop no disponible todavía
            }
            _loaded = true;
        }

        public async Task SetUserAsync(POS.Shared.DTOs.AuthResponse response)
        {
            Token = response.Token;
            UserId = response.UserId;
            Email = response.Email;
            FullName = response.FullName;
            Role = response.Role;
            await PersistAsync();
            OnChange?.Invoke();
        }

        public async Task LogoutAsync()
        {
            Token = null;
            UserId = 0;
            Email = null;
            FullName = null;
            Role = null;
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "pos_token");
                await _js.InvokeVoidAsync("localStorage.removeItem", "pos_userId");
                await _js.InvokeVoidAsync("localStorage.removeItem", "pos_email");
                await _js.InvokeVoidAsync("localStorage.removeItem", "pos_fullname");
                await _js.InvokeVoidAsync("localStorage.removeItem", "pos_role");
            }
            catch { }
            OnChange?.Invoke();
        }

        private static bool IsTokenExpired(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    return true;
                }
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var document = System.Text.Json.JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("exp", out var exp))
                {
                    return true;
                }
                return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        private async Task PersistAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", "pos_token", Token ?? "");
                await _js.InvokeVoidAsync("localStorage.setItem", "pos_userId", UserId.ToString());
                await _js.InvokeVoidAsync("localStorage.setItem", "pos_email", Email ?? "");
                await _js.InvokeVoidAsync("localStorage.setItem", "pos_fullname", FullName ?? "");
                await _js.InvokeVoidAsync("localStorage.setItem", "pos_role", Role ?? "");
            }
            catch { }
        }
    }
}
