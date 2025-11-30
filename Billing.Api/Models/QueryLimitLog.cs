namespace Billing.Api.Models
{
    public class QueryLimitLog
    {
        public int Id { get; set; }

        public string SubscriberNo { get; set; } = string.Empty;

        // Sadece günü umursuyoruz (saat/dakika değil)
        public DateTime Date { get; set; }

        public int Count { get; set; }
    }
}

