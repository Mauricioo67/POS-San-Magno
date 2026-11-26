using System;
using System.Collections.Generic;
using POS.Shared.Enums;

namespace POS.Shared.Entities
{
    public class Dispatch
    {
        public int Id { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public DateTime DispatchDate { get; set; } = DateTime.Now;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null;
        public int UserId { get; set; }
        public User User { get; set; } = null;
        public DispatchStatus Status { get; set; } = DispatchStatus.Pendiente;
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public ICollection<DispatchDetail> DispatchDetails { get; set; } = new List<DispatchDetail>();
    }
}
