namespace Billing.Api.Dtos
{
    public class BillSummaryDto
    {
        public string SubscriberNo { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal BillTotal { get; set; }
        public bool IsPaid { get; set; }
    }
}
