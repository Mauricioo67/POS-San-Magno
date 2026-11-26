using System.Collections.Generic;

namespace POS.Shared.DTOs
{
    public class DispatchRequest
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<DispatchItemDto> Items { get; set; } = new List<DispatchItemDto>();
    }
}
