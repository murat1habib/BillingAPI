using Billing.Api.Models;

namespace Billing.Api.Data
{
    public static class SeedData
    {
        public static void Initialize(BillingDbContext context)
        {
            // ✅ Subscribers: upsert gibi çalış
            EnsureSubscriber(context, "1001", "Ali Veli", "ali@example.com");
            EnsureSubscriber(context, "1002", "Ayşe Yılmaz", "ayse@example.com");
            EnsureSubscriber(context, "1003", "Murat Okan", "murat@example.com");
            EnsureSubscriber(-context, "1004", "Talha Chiara", "talha@example.com");

            context.SaveChanges();

            // ✅ Bills: aynı ay/yıl varsa tekrar ekleme
            EnsureBill(context, "1001", 2024, 10, 300, 0, true,
                new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 120 },
                new BillDetail { Description = "SMS", ItemType = "SMS", Amount = 30 },
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 150 }
            );

            EnsureBill(context, "1001", 2024, 9, 250, 250, true,
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 200 },
                new BillDetail { Description = "Other", ItemType = "Other", Amount = 50 }
            );

            EnsureBill(context, "1002", 2024, 10, 180, 0, false,
                new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 80 },
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 100 }
            );

            EnsureBill(context, "1003", 2024, 10, 1800, 0, true,
                new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 800 },
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 1000 }
            );

            EnsureBill(context, "1003", 2024, 9, 2000, 0, true,
                new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 900 },
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 1100 }
            );

            EnsureBill(context, "1004", 2025, 9, 100000, 0, false,
                new BillDetail { Description = "Voice calls", ItemType = "Call", Amount = 0 },
                new BillDetail { Description = "Internet", ItemType = "Data", Amount = 10000 }
            );




            context.SaveChanges();
        }

        private static void EnsureSubscriber(BillingDbContext context, string no, string name, string email)
        {
            var existing = context.Subscribers.FirstOrDefault(s => s.SubscriberNo == no);
            if (existing != null) return;

            context.Subscribers.Add(new Subscriber
            {
                SubscriberNo = no,
                Name = name,
                Email = email
            });
        }

        private static void EnsureBill(
            BillingDbContext context,
            string subscriberNo,
            int year,
            int month,
            decimal totalAmount,
            decimal paidAmount,
            bool isPaid,
            params BillDetail[] details
        )
        {
            // aynı subscriber+year+month varsa ekleme
            var exists = context.Bills.Any(b =>
                b.Subscriber.SubscriberNo == subscriberNo &&
                b.Year == year &&
                b.Month == month
            );
            if (exists) return;

            var subscriber = context.Subscribers.First(s => s.SubscriberNo == subscriberNo);

            var bill = new Bill
            {
                Subscriber = subscriber,
                Year = year,
                Month = month,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                IsPaid = isPaid
            };

            foreach (var d in details)
                bill.Details.Add(d);

            context.Bills.Add(bill);
        }
    }
}
