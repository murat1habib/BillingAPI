using System.ComponentModel.DataAnnotations;

namespace Billing.Api.Dtos
{
    public class AdminAddBillRequestDto
    {
        [Required]
        public string SubscriberNo { get; set; } = string.Empty;

        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(1, double.MaxValue)]
        public decimal TotalAmount { get; set; }
    }
}

