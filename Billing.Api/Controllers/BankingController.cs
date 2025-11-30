using Billing.Api.Data;
using Billing.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace Billing.Api.Controllers
{
    [ApiController]
    [Route("api/v1/banking")]
    [Authorize(Roles = "bank")]
    public class BankingController : ControllerBase
    {
        private readonly BillingDbContext _context;

        public BankingController(BillingDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/banking/query-bill
        // Banking App - Query Bill: returns all unpaid bills by month
        [HttpGet("query-bill")]
        public async Task<ActionResult<List<UnpaidBillDto>>> QueryUnpaidBills(
            [FromQuery] string subscriberNo)
        {
            var bills = await _context.Bills
                .Include(b => b.Subscriber)
                .Where(b => b.Subscriber!.SubscriberNo == subscriberNo && !b.IsPaid)
                .OrderBy(b => b.Year)
                .ThenBy(b => b.Month)
                .ToListAsync();


            var dtos = bills.Select(b => new UnpaidBillDto
            {
                SubscriberNo = subscriberNo,
                Year = b.Year,
                Month = b.Month,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                RemainingAmount = b.TotalAmount - b.PaidAmount
            }).ToList();

            return Ok(dtos);
        }
    }
}

