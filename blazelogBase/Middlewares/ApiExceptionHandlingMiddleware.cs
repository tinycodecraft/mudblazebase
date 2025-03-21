using blazelogBase.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text;

namespace blazelogBase.Middlewares;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {


        var requestContent = new StringBuilder();
        try
        {

            requestContent.AppendLine("=== Error happens to Request Info ===");
            requestContent.AppendLine($"method = {context.Request.Method.ToUpper()}");
            requestContent.AppendLine($"path = {context.Request.Path}");

            requestContent.AppendLine("-- headers");
            foreach (var (headerKey, headerValue) in context.Request.Headers)
            {
                requestContent.AppendLine($"header = {headerKey}    value = {headerValue}");
            }
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, requestContent);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, StringBuilder sb)
    {
        string result;

        _logger.LogError(sb.ToString());

        if (ex is DomainException e)
        {
            var problemDetails = new CustomValidationProblemDetails(new List<CustomError> { new() { Code = e.Code, Message = e.Message } })
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path,
            };
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            result = JsonSerializer.Serialize(problemDetails);
        }
        else
        {
            _logger.LogError(ex, $"An unhandled exception has occurred, {ex.Message}");
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error.",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.Path,
                Detail = "Internal server error occurred!"
            };
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            result = JsonSerializer.Serialize(problemDetails);
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
}