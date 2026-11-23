using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace StockPortal.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Print incoming request banner to Console
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n--> [API REQUEST] {DateTime.Now:HH:mm:ss} | {method} {path}{query} | From: {ip}");
            Console.ForegroundColor = prevColor;

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;

                // Color based on status code
                Console.ForegroundColor = statusCode switch
                {
                    >= 200 and < 300 => ConsoleColor.Green,
                    >= 400 and < 500 => ConsoleColor.Yellow,
                    >= 500 => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"<-- [API RESPONSE] {DateTime.Now:HH:mm:ss} | {method} {path} => {statusCode} | Elapsed: {sw.ElapsedMilliseconds} ms\n");
                Console.ForegroundColor = prevColor;

                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    method, path, statusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}
