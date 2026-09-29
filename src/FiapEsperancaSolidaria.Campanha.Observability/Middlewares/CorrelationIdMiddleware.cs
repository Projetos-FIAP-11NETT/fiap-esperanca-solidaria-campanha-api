using FiapEsperancaSolidaria.Campanha.Observability.Correlation;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace FiapEsperancaSolidaria.Campanha.Observability.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdAccessor.HeaderName, out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Items[CorrelationIdAccessor.HeaderName] = correlationId;
        context.Response.Headers[CorrelationIdAccessor.HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
