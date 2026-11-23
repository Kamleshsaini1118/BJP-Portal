using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StockPortal.Core.Common;

namespace StockPortal.API.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing request: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Handle SQL Duplicate Key / Unique Constraint violations (2601, 2627)
            if (exception is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                var duplicateResponse = ApiResponse<object>.Fail("Duplicate data detected: A record with this unique code or information already exists in the system.");
                var duplicateJson = JsonSerializer.Serialize(duplicateResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(duplicateJson);
                return;
            }

            if (exception is InvalidOperationException invEx)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var invResponse = ApiResponse<object>.Fail(invEx.Message);
                var invJson = JsonSerializer.Serialize(invResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(invJson);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.Fail("An internal server error occurred. Please contact the administrator.");
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
