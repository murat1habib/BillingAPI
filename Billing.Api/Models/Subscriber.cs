namespace Billing.Api.Models
{
    public class Subscriber
    {
        public int Id { get; set; }               // PK (EF için)

        public string SubscriberNo { get; set; } = string.Empty; // Operatör abone numarası

        public string? Name { get; set; }         
        public string? Email { get; set; }        

        // Navigation property (ileri için)
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}
