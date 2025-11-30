using System.Collections.Generic;

namespace Billing.Api.Dtos
{
    public class BillDetailedDto
    {
        public string SubscriberNo { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal BillTotal { get; set; }
        public bool IsPaid { get; set; }

        // Paging info
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalDetailCount { get; set; }

        public List<BillDetailDto> Details { get; set; } = new();
    }
}

