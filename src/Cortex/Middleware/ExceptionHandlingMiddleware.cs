using Cortex.Shared;
using Cortex.Shared.Exceptions;
using System.Net;
using System.Text.Json;

namespace Cortex.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions and returns standardized error responses.
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, errors) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, new[] { notFound.Message }),
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Errors.SelectMany(e => e.Value).ToArray()),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, new[] { forbidden.Message }),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, new[] { "Unauthorized access." }),
            _ => (HttpStatusCode.InternalServerError, new[] { "An unexpected error occurred.", $"Trace ID: {traceId}" })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
        else
            _logger.LogWarning(exception, "Handled exception: {StatusCode}. TraceId: {TraceId}", statusCode, traceId);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
