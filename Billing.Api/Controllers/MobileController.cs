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

        // GET: api/v1/mobile/query-bill-detailed
        // Query Bill Detailed: SubscriberNo + Year + Month + paging for bill details
        [HttpGet("query-bill-detailed")]
        public async Task<ActionResult<BillDetailedDto>> QueryBillDetailed(
            [FromQuery] string subscriberNo,
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            // (İstersen rate limit'i buraya da kopyalayabiliriz; şimdilik minimal tutuyoruz)

            var bill = await _context.Bills
                .Include(b => b.Subscriber)
                .Include(b => b.Details)
                .FirstOrDefaultAsync(b =>
                    b.Subscriber!.SubscriberNo == subscriberNo &&
                    b.Year == year &&
                    b.Month == month);

            if (bill == null)
                return NotFound($"Bill not found for subscriber {subscriberNo} {year}-{month:D2}");

            var totalDetailCount = bill.Details.Count;

            var paged = bill.Details
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new BillDetailDto
                {
                    Id = d.Id,
                    Description = d.Description,
                    ItemType = d.ItemType,
                    Amount = d.Amount
                })
                .ToList();

            var dto = new BillDetailedDto
            {
                SubscriberNo = subscriberNo,
                Year = bill.Year,
                Month = bill.Month,
                BillTotal = bill.TotalAmount,
                IsPaid = bill.IsPaid,
                Page = page,
                PageSize = pageSize,
                TotalDetailCount = totalDetailCount,
                Details = paged
            };

            return Ok(dto);
        }

    }
}

