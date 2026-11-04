
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into a predictable JSON error contract so the
/// API never leaks stack traces or provider details to clients.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _logger.LogWarning(
                exception,
                "Unique constraint violation on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                "This record conflicts with data that already exists.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database update failed on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                "The change could not be saved. Please review the data and try again.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            // 2601: duplicate key in a unique index, 2627: unique constraint.
            if (current is Microsoft.Data.SqlClient.SqlException sqlException &&
                sqlException.Errors.Cast<Microsoft.Data.SqlClient.SqlError>()
                    .Any(error => error.Number is 2601 or 2627))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(
            new { message },
            SerializerOptions);

        await context.Response.WriteAsync(payload);
    }
}
