namespace Billing.Api.Dtos
{
    public class UnpaidBillDto
    {
        public string SubscriberNo { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}

