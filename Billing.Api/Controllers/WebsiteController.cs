using Billing.Api.Data;
using Billing.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace Billing.Api.Controllers
{
    [ApiController]
    [Route("api/v1/website")]
    [AllowAnonymous]
    public class WebsiteController : ControllerBase
    {
        private readonly BillingDbContext _context;

        public WebsiteController(BillingDbContext context)
        {
            _context = context;
        }

        // POST: api/v1/website/pay-bill
        // Pay Bill: Marks bill as paid (supports partial payments)
        [HttpPost("pay-bill")]
        public async Task<ActionResult<PayBillResponseDto>> PayBill([FromBody] PayBillRequestDto request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new PayBillResponseDto
                {
                    PaymentStatus = "Error",
                    Message = "Amount must be greater than zero."
                });
            }

            var bill = await _context.Bills
                .Include(b => b.Subscriber)
                .FirstOrDefaultAsync(b =>
                    b.Subscriber!.SubscriberNo == request.SubscriberNo &&
                    b.Year == request.Year &&
                    b.Month == request.Month);

            if (bill == null)
            {
                return NotFound(new PayBillResponseDto
                {
                    PaymentStatus = "Error",
                    Message = $"Bill not found for subscriber {request.SubscriberNo} {request.Year}-{request.Month:D2}"
                });
            }

            if (bill.IsPaid)
            {
                return Ok(new PayBillResponseDto
                {
                    PaymentStatus = "Error",
                    Message = "Bill is already fully paid.",
                    TotalAmount = bill.TotalAmount,
                    PaidAmount = bill.PaidAmount,
                    RemainingAmount = bill.TotalAmount - bill.PaidAmount
                });
            }

            // Kısmi ödeme / tam ödeme
            bill.PaidAmount += request.Amount;

            if (bill.PaidAmount >= bill.TotalAmount)
            {
                bill.PaidAmount = bill.TotalAmount;
                bill.IsPaid = true;
            }

            await _context.SaveChangesAsync();

            var response = new PayBillResponseDto
            {
                PaymentStatus = "Successful",
                Message = bill.IsPaid
                    ? "Bill fully paid."
                    : "Partial payment completed.",
                TotalAmount = bill.TotalAmount,
                PaidAmount = bill.PaidAmount,
                RemainingAmount = bill.TotalAmount - bill.PaidAmount
            };

            return Ok(response);
        }
    }
}

