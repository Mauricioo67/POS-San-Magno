using POS.Shared.Enums;

namespace POS.Shared.DTOs
{
    public class StockMovementRequest
    {
        public int ProductId { get; set; }
        public MovementType MovementType { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
