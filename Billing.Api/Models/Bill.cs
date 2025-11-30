namespace Billing.Api.Models
{
    public class Bill
    {
        public int Id { get; set; }                 // PK

        public int SubscriberId { get; set; }       // FK
        public Subscriber? Subscriber { get; set; } // Navigation

        public int Year { get; set; }               // 2024
        public int Month { get; set; }              // 1–12

        public decimal TotalAmount { get; set; }    // Toplam fatura
        public decimal PaidAmount { get; set; }     // Şimdiye kadar ödenen
        public bool IsPaid { get; set; }            // Tamamen ödendiyse true

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BillDetail> Details { get; set; } = new List<BillDetail>();
    }
}

