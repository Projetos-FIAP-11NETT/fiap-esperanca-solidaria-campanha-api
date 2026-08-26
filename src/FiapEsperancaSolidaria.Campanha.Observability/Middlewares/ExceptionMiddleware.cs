using System.Net;
using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FiapEsperancaSolidaria.Campanha.Observability.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                string.Join(" | ", validationException.Errors.Select(e => e.ErrorMessage))),
            DomainException domainException => (HttpStatusCode.BadRequest, domainException.Message),
            NotFoundException notFoundException => (HttpStatusCode.NotFound, notFoundException.Message),
            BusinessException businessException => (HttpStatusCode.UnprocessableEntity, businessException.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado."),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Erro não tratado ao processar a requisição {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Erro de negócio ao processar {Path}: {Message}", context.Request.Path, message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(payload);
    }
}
