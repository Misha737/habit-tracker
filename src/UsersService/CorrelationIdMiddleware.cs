namespace UsersService;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";
        
        if (!context.Request.Headers.TryGetValue(headerName, out var correlationId) 
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            _logger.LogDebug("Generated new CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogDebug("Received existing CorrelationId: {CorrelationId}", correlationId);
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        
        using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            await _next(context);
        }

        if (!context.Response.Headers.ContainsKey(headerName))
        {
            context.Response.Headers.Append(headerName, correlationId.ToString());
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
