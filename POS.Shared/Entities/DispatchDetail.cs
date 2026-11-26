using System.Text.Json.Serialization;

namespace POS.Shared.Entities
{
    public class DispatchDetail
    {
        public int Id { get; set; }
        public int DispatchId { get; set; }

        [JsonIgnore]
        public Dispatch Dispatch { get; set; } = null;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
