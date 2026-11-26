using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using POS.Client.Services;

namespace POS.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Moneda del sistema: bolivianos (Bs). Sin esto, ToString("C") usa la
            // cultura del navegador y muestra S/, $ u otro símbolo.
            var bolivia = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            bolivia.NumberFormat.CurrencySymbol = "Bs";
            bolivia.NumberFormat.CurrencyPositivePattern = 2; // "Bs 3,50"
            bolivia.NumberFormat.CurrencyNegativePattern = 9; // "-Bs 3,50"
            bolivia.NumberFormat.CurrencyDecimalSeparator = ",";
            bolivia.NumberFormat.CurrencyGroupSeparator = ".";
            bolivia.NumberFormat.NumberDecimalSeparator = ",";
            bolivia.NumberFormat.NumberGroupSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = bolivia;
            CultureInfo.DefaultThreadCurrentUICulture = bolivia;
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // En desarrollo local el backend usa HTTPS en el puerto 7008.
            // Esto evita que una configuración antigua apunte al puerto HTTP 5004,
            // cuyo redireccionamiento HTTPS bloquea las solicitudes preflight CORS.
            var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
                ? "https://localhost:7008/"
                : builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
            {
                throw new InvalidOperationException($"ApiBaseUrl no es una URL válida: {apiBaseUrl}");
            }

            Console.WriteLine($"POS.Client conectará con la API: {apiUri}");

            builder.Services.AddScoped(sp =>
            {
                var handler = new AuthHeaderHandler(sp.GetRequiredService<AuthStateService>())
                {
                    InnerHandler = new HttpClientHandler()
                };
                return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
            });

            builder.Services.AddMudServices();
            builder.Services.AddScoped<AuthStateService>();
            builder.Services.AddScoped<AuthApiService>();
            builder.Services.AddScoped<UserApiService>();
            builder.Services.AddScoped<CategoryApiService>();
            builder.Services.AddScoped<ProductApiService>();
            builder.Services.AddScoped<SaleApiService>();
            builder.Services.AddScoped<StockApiService>();
            builder.Services.AddScoped<CashClosingApiService>();
            builder.Services.AddScoped<CustomerApiService>();
            builder.Services.AddScoped<DispatchApiService>();
            builder.Services.AddScoped<StockAlertService>();

            await builder.Build().RunAsync();
        }
    }
}
