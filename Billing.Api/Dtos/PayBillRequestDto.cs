namespace Billing.Api.Dtos
{
    public class PayBillRequestDto
    {
        public string SubscriberNo { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }

        // Ödenmek istenen tutar (kısmi ödeme olabilir)
        public decimal Amount { get; set; }
    }
}

