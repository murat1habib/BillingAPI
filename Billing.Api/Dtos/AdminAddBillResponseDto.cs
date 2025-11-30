namespace Billing.Api.Dtos
{
    public class AdminAddBillResponseDto
    {
        // "Success" veya "Error"
        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? BillId { get; set; }
    }
}

