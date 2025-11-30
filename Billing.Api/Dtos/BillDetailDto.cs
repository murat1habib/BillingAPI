namespace Billing.Api.Dtos
{
    public class BillDetailDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public decimal Amount { get; set; }
    }
}
