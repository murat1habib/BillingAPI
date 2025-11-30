namespace Billing.Api.Models
{
    public class BillDetail
    {
        public int Id { get; set; }          // PK

        public int BillId { get; set; }      // FK
        public Bill? Bill { get; set; }      // Navigation

        public string Description { get; set; } = string.Empty; // Örn: “Internet usage”
        public string? ItemType { get; set; }                    // Örn: Call/SMS/Data

        public decimal Amount { get; set; }  // Bu kalemin tutarı
    }
}
