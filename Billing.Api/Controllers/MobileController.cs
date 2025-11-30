using Billing.Api.Data;
using Billing.Api.Dtos;
using Billing.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;


namespace Billing.Api.Controllers
{
    [ApiController]
    [Route("api/v1/mobile")]
    [Authorize(Roles = "mobile")]
    public class MobileController : ControllerBase
    {
        private readonly BillingDbContext _context;

        public MobileController(BillingDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/mobile/query-bill
        // Query Bill: SubscriberNo + Year + Month + paging
        [HttpGet("query-bill")]
        public async Task<ActionResult<BillSummaryDto>> QueryBill(
    [FromQuery] string subscriberNo,
    [FromQuery] int year,
    [FromQuery] int month)
        {
            // === Rate limiting: same subscriber, max 3 queries per day ===
            var today = DateTime.UtcNow.Date;

            var log = await _context.QueryLimitLogs
                .FirstOrDefaultAsync(x =>
                    x.SubscriberNo == subscriberNo &&
                    x.Date == today);

            if (log == null)
            {
                // İlk sorgu
                log = new QueryLimitLog
                {
                    SubscriberNo = subscriberNo,
                    Date = today,
                    Count = 1
                };
                _context.QueryLimitLogs.Add(log);
            }
            else
            {
                if (log.Count >= 3)
                {
                    // Limit aşıldı → 429 Too Many Requests
                    return StatusCode(StatusCodes.Status429TooManyRequests,
                        $"Daily query limit exceeded for subscriber {subscriberNo}. Max 3 per day.");
                }

                log.Count++;
            }

            await _context.SaveChangesAsync();
            // === Rate limit kontrolü bitti ===

            var bill = await _context.Bills
                .Include(b => b.Subscriber)
                .FirstOrDefaultAsync(b =>
                    b.Subscriber!.SubscriberNo == subscriberNo &&
                    b.Year == year &&
                    b.Month == month);

            if (bill == null)
            {
                return NotFound($"Bill not found for subscriber {subscriberNo} {year}-{month:D2}");
            }

            var dto = new BillSummaryDto
            {
                SubscriberNo = subscriberNo,
                Year = bill.Year,
                Month = bill.Month,
                BillTotal = bill.TotalAmount,
                IsPaid = bill.IsPaid
            };

            return Ok(dto);
        }
    }
}

