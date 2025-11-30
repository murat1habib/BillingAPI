namespace Billing.Api.Dtos
{
    public class AdminBatchRowErrorDto
    {
        public int LineNumber { get; set; }
        public string RawLine { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
