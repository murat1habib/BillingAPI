using System.Collections.Generic;

namespace Billing.Api.Dtos
{
    public class AdminBatchAddBillResultDto
    {
        public int TotalLines { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }

        public List<AdminBatchRowErrorDto> Errors { get; set; } = new();
    }
}

