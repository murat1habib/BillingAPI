namespace Billing.Api.Dtos
{
    public class PayBillResponseDto
    {
        // "Successful" veya "Error"
        public string PaymentStatus { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public decimal? TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? RemainingAmount { get; set; }
    }
}

