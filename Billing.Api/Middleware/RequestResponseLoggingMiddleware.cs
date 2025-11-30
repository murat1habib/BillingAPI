using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Billing.Api.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var stopwatch = Stopwatch.StartNew();
            var requestTime = DateTime.UtcNow;

            // Request info
            var method = request.Method;
            var path = request.Path + request.QueryString;
            var sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var headers = string.Join("; ",
                request.Headers.Select(h => $"{h.Key}={h.Value}"));

            var requestSize = request.ContentLength ?? 0;

            // Auth başarı/başarısız bilgisi (claims varsa başarılı varsayıyoruz)
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

            var originalBodyStream = response.Body;
            await using var responseBody = new MemoryStream();
            response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                responseBody.Seek(0, SeekOrigin.Begin);
                var responseSize = responseBody.Length;
                responseBody.Seek(0, SeekOrigin.Begin);

                var statusCode = response.StatusCode;
                var latencyMs = stopwatch.ElapsedMilliseconds;

                var logBuilder = new StringBuilder();
                logBuilder.AppendLine("=== HTTP Request/Response Log ===");
                logBuilder.AppendLine($"Timestamp (UTC): {requestTime:o}");
                logBuilder.AppendLine($"Method: {method}");
                logBuilder.AppendLine($"Path: {path}");
                logBuilder.AppendLine($"Source IP: {sourceIp}");
                logBuilder.AppendLine($"Authenticated: {isAuthenticated}");
                logBuilder.AppendLine($"Request size (bytes): {requestSize}");
                logBuilder.AppendLine($"Response status: {statusCode}");
                logBuilder.AppendLine($"Response size (bytes): {responseSize}");
                logBuilder.AppendLine($"Latency (ms): {latencyMs}");
                logBuilder.AppendLine($"Headers: {headers}");
                logBuilder.AppendLine("=================================");

                _logger.LogInformation(logBuilder.ToString());

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                response.Body = originalBodyStream;
            }
        }
    }

    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
