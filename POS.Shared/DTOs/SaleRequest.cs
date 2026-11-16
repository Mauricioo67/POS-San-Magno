using System.Collections.Generic;
using POS.Shared.Enums;

namespace POS.Shared.DTOs
{
    public class SaleRequest
    {
        public int UserId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
    }
}
