using Billing.Api.Data;
using Billing.Api.Dtos;
using Billing.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Authorization;


namespace Billing.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly BillingDbContext _context;

        public AdminController(BillingDbContext context)
        {
            _context = context;
        }

        // POST: api/v1/admin/add-bill
        // Admin - Add Bill: Adds a bill for a month for given subscriber
        [HttpPost("add-bill")]
        public async Task<ActionResult<AdminAddBillResponseDto>> AddBill(
            [FromBody] AdminAddBillRequestDto request)
        {
            if (request.TotalAmount <= 0)
            {
                return BadRequest(new AdminAddBillResponseDto
                {
                    Status = "Error",
                    Message = "TotalAmount must be greater than zero."
                });
            }

            // Abone var mı?
            var subscriber = await _context.Subscribers
                .FirstOrDefaultAsync(s => s.SubscriberNo == request.SubscriberNo);

            if (subscriber == null)
            {
                return NotFound(new AdminAddBillResponseDto
                {
                    Status = "Error",
                    Message = $"Subscriber not found: {request.SubscriberNo}"
                });
            }

            // Aynı yıl + ay için fatura zaten var mı?
            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b =>
                    b.SubscriberId == subscriber.Id &&
                    b.Year == request.Year &&
                    b.Month == request.Month);

            if (existingBill != null)
            {
                return Conflict(new AdminAddBillResponseDto
                {
                    Status = "Error",
                    Message = "Bill already exists for this subscriber and month.",
                    BillId = existingBill.Id
                });
            }

            var bill = new Bill
            {
                SubscriberId = subscriber.Id,
                Year = request.Year,
                Month = request.Month,
                TotalAmount = request.TotalAmount,
                PaidAmount = 0,
                IsPaid = false
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return Ok(new AdminAddBillResponseDto
            {
                Status = "Success",
                Message = "Bill created successfully.",
                BillId = bill.Id
            });
        }

        [HttpPost("add-bill-batch")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<ActionResult<AdminBatchAddBillResultDto>> AddBillBatch(
    IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("CSV file is required.");

            var result = new AdminBatchAddBillResultDto
            {
                Errors = new List<AdminBatchRowErrorDto>()
            };

            try
            {
                // 1) Tüm satırları hafızaya al (çok büyük dosyalarda stream + batch işle)
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var lines = new List<string>();
                string? line;
                int lineNumber = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (lineNumber == 1 && line.StartsWith("SubscriberNo", StringComparison.OrdinalIgnoreCase))
                        continue;

                    lines.Add(line);
                }

                result.TotalLines = lines.Count;

                // 2) İhtiyaç duyulan SubscriberNo seti
                var wantedNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in lines)
                {
                    var parts = raw.Split(',');
                    if (parts.Length < 4) continue;
                    wantedNos.Add(parts[0].Trim());
                }

                // 3) Subscriber’ları tek seferde çek
                var subscribers = await _context.Subscribers
                    .Where(s => wantedNos.Contains(s.SubscriberNo))
                    .ToListAsync(cancellationToken);

                var subByNo = subscribers.ToDictionary(s => s.SubscriberNo, StringComparer.OrdinalIgnoreCase);

                // 4) Önce parse edip geçerli kayıtları biriktir
                var candidateBills = new List<Bill>();

                lineNumber = 0;
                foreach (var raw in lines)
                {
                    lineNumber++;

                    var parts = raw.Split(',');
                    if (parts.Length < 4)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new AdminBatchRowErrorDto
                        {
                            LineNumber = lineNumber,
                            RawLine = raw,
                            ErrorMessage = "Line must have 4 columns: SubscriberNo,Year,Month,TotalAmount"
                        });
                        continue;
                    }

                    var subscriberNo = parts[0].Trim();

                    if (!int.TryParse(parts[1], out var year) ||
                        !int.TryParse(parts[2], out var month) ||
                        !decimal.TryParse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var totalAmount))
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new AdminBatchRowErrorDto
                        {
                            LineNumber = lineNumber,
                            RawLine = raw,
                            ErrorMessage = "Year, Month or TotalAmount has invalid format. (Use dot for decimals)"
                        });
                        continue;
                    }

                    if (!subByNo.TryGetValue(subscriberNo, out var subscriber))
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new AdminBatchRowErrorDto
                        {
                            LineNumber = lineNumber,
                            RawLine = raw,
                            ErrorMessage = $"Subscriber not found: {subscriberNo}"
                        });
                        continue;
                    }

                    candidateBills.Add(new Bill
                    {
                        SubscriberId = subscriber.Id,
                        Year = year,
                        Month = month,
                        TotalAmount = totalAmount,
                        PaidAmount = 0,
                        IsPaid = false
                    });
                }

                // 5) Var olan faturaları tek seferde sorgula ve filtrele
                var subsIds = candidateBills.Select(b => b.SubscriberId).Distinct().ToList();
                var years = candidateBills.Select(b => b.Year).Distinct().ToList();
                var months = candidateBills.Select(b => b.Month).Distinct().ToList();

                var existing = await _context.Bills
                    .Where(b => subsIds.Contains(b.SubscriberId)
                                && years.Contains(b.Year)
                                && months.Contains(b.Month))
                    .Select(b => new { b.SubscriberId, b.Year, b.Month })
                    .ToListAsync(cancellationToken);

                var existingSet = new HashSet<(int sid, int y, int m)>(
                    existing.Select(e => (e.SubscriberId, e.Year, e.Month)));

                foreach (var bill in candidateBills)
                {
                    if (existingSet.Contains((bill.SubscriberId, bill.Year, bill.Month)))
                    {
                        // Satır numarasını bulmak için (gerekirse) map tutabilirsiniz; burada kısa geçiyoruz
                        result.ErrorCount++;
                        result.Errors.Add(new AdminBatchRowErrorDto
                        {
                            LineNumber = 0,
                            RawLine = "",
                            ErrorMessage = "Bill already exists for this subscriber and month."
                        });
                        continue;
                    }

                    _context.Bills.Add(bill);
                    result.SuccessCount++;
                }

                await _context.SaveChangesAsync(cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                // İstek iptal edildiyse daha temiz mesaj
                return StatusCode(499, "Client closed request.");
            }
            catch (InvalidDataException ex) // Body length limit gibi
            {
                return BadRequest($"Invalid multipart/form-data: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Geliştirme dışında iç hatayı dökmeyin; burada kısa tutuyoruz
                return StatusCode(500, "Unexpected error while processing CSV.");
            }
        }
    }
}

