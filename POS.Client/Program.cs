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
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

            builder.Services.AddMudServices();
            builder.Services.AddScoped<AuthStateService>();
            builder.Services.AddScoped<AuthApiService>();
            builder.Services.AddScoped<UserApiService>();
            builder.Services.AddScoped<CategoryApiService>();
            builder.Services.AddScoped<ProductApiService>();
            builder.Services.AddScoped<SaleApiService>();
            builder.Services.AddScoped<StockApiService>();
            builder.Services.AddScoped<CashClosingApiService>();

            await builder.Build().RunAsync();
        }
    }
}
