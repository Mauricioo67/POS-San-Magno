using POS.Shared.Enums;
using System;
using System.Collections.Generic;

namespace POS.Shared.Entities
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public User User { get; set; } = null;
        public int UserId { get; set; }
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public PaymentMethod PaymentMethod { get; set; }
    }
}
