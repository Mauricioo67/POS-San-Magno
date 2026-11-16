namespace POS.Shared.DTOs
{
    public class CashClosingRequest
    {
        public decimal InitialBalance { get; set; }
        public decimal TotalAdjustments { get; set; }
        public int UserId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
