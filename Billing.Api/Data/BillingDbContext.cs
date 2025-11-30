using Billing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Data
{
    public class BillingDbContext : DbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options)
            : base(options)
        {
        }

        public DbSet<QueryLimitLog> QueryLimitLogs { get; set; } = null!;
        public DbSet<Subscriber> Subscribers { get; set; } = null!;
        public DbSet<Bill> Bills { get; set; } = null!;
        public DbSet<BillDetail> BillDetails { get; set; } = null!;
    }
}

