using Billing.Api.Models;

namespace Billing.Api.Data
{
    public static class SeedData
    {
        public static void Initialize(BillingDbContext context)
        {
            if (context.Subscribers.Any())
            {
                return;
            }

            var sub1 = new Subscriber
            {
                SubscriberNo = "1001",
                Name = "Ali Veli",
                Email = "ali@example.com"
            };

            var sub2 = new Subscriber
            {
                SubscriberNo = "1002",
                Name = "Ayşe Yılmaz",
                Email = "ayse@example.com"
            };

            var bill1 = new Bill
            {
                Subscriber = sub1,
                Year = 2024,
                Month = 10,
                TotalAmount = 300,
                PaidAmount = 0,
                IsPaid = false,
                Details =
                {
                    new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 120 },
                    new BillDetail { Description = "SMS", ItemType = "SMS", Amount = 30 },
                    new BillDetail { Description = "Internet", ItemType = "Data", Amount = 150 }
                }
            };

            var bill2 = new Bill
            {
                Subscriber = sub1,
                Year = 2024,
                Month = 9,
                TotalAmount = 250,
                PaidAmount = 250,
                IsPaid = true,
                Details =
                {
                    new BillDetail { Description = "Internet", ItemType = "Data", Amount = 200 },
                    new BillDetail { Description = "Other", ItemType = "Other", Amount = 50 }
                }
            };

            var bill3 = new Bill
            {
                Subscriber = sub2,
                Year = 2024,
                Month = 10,
                TotalAmount = 180,
                PaidAmount = 0,
                IsPaid = false,
                Details =
                {
                    new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 80 },
                    new BillDetail { Description = "Internet", ItemType = "Data", Amount = 100 }
                }
            };

            context.Subscribers.AddRange(sub1, sub2);
            context.Bills.AddRange(bill1, bill2, bill3);

            context.SaveChanges();
        }
    }
}

