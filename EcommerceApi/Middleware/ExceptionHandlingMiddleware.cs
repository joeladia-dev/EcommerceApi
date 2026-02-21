using System.Net;
using EcommerceApi.Common;

namespace EcommerceApi.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ArgumentNullException =>
                new { success = false, message = "An argument was null or invalid", errors = new[] { exception.Message } },
            ArgumentException =>
                new { success = false, message = "An invalid argument was provided", errors = new[] { exception.Message } },
            KeyNotFoundException =>
                new { success = false, message = "The requested resource was not found", errors = new[] { exception.Message } },
            _ =>
                new { success = false, message = "An unexpected error occurred", errors = new[] { "Internal server error" } }
        };

        context.Response.StatusCode = exception switch
        {
            ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
