using POS.Shared.Entities;

namespace POS.Client.Services
{
    /// <summary>
    /// Mantiene el estado de los productos con stock bajo o agotado para que
    /// cualquier página pueda avisar sin repetir la consulta.
    /// </summary>
    public class StockAlertService
    {
        private readonly ProductApiService _productApi;
        private bool _loading;

        public StockAlertService(ProductApiService productApi)
        {
            _productApi = productApi;
        }

        public List<Product> CriticalProducts { get; private set; } = new();

        public int Count => CriticalProducts.Count;

        public int OutOfStockCount => CriticalProducts.Count(p => p.CurrentStock <= 0);

        public bool HasAlerts => CriticalProducts.Count > 0;

        public event Action? Changed;

        public async Task RefreshAsync()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            try
            {
                var products = await _productApi.GetAllAsync();
                CriticalProducts = products
                    .Where(p => p.IsActive && p.CurrentStock <= p.StockMinimun)
                    .OrderBy(p => p.CurrentStock)
                    .ThenBy(p => p.Name)
                    .ToList();
            }
            catch
            {
                // Sin sesión o sin conexión: no interrumpir la navegación.
                return;
            }
            finally
            {
                _loading = false;
            }

            Changed?.Invoke();
        }
    }
}
