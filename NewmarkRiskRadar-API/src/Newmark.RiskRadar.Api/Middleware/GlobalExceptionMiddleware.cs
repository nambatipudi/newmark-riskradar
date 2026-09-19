using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newmark.RiskRadar.Application.Exceptions;
using Newmark.RiskRadar.Domain.Exceptions;

namespace Newmark.RiskRadar.Api.Middleware;

/// <summary>
/// Converts any unhandled exception into an RFC 7807 problem document so the UI never has to
/// parse an HTML error page, and so stack traces stay out of non-development responses.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client hung up; nothing to report.
        }
        catch (Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            logger.LogError(exception, "Unhandled exception on {Method} {Path}. TraceId {TraceId}.",
                context.Request.Method, context.Request.Path, traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, title, detail) = exception switch
            {
                InvalidFinancialInputException financial =>
                    (StatusCodes.Status400BadRequest, "Invalid financial input.", financial.Message),
                ArgumentOutOfRangeException or ArgumentException =>
                    (StatusCodes.Status400BadRequest, "Invalid request.", exception.Message),
                KeyNotFoundException =>
                    (StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
                TapeSyncConflictException =>
                    (StatusCodes.Status409Conflict, "Servicing tape is locked.", exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
            };

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://httpstatuses.io/{status}",
                Instance = context.Request.Path,
                // Domain guard messages are authored by us and safe to return; only server faults are withheld.
                Detail = detail ?? (environment.IsDevelopment() ? exception.ToString() : null)
            };
            problem.Extensions["traceId"] = traceId;

            if (exception is InvalidFinancialInputException invalidInput)
            {
                problem.Extensions["parameterName"] = invalidInput.ParameterName;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            await context.Response
                .WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions), context.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
