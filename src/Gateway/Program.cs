using Gateway;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(ctx =>
    {
        ctx.AddRequestTransform(async transformCtx =>
        {
            const string header = "X-Correlation-Id";

            if (transformCtx.HttpContext.Items.TryGetValue("CorrelationId", out var correlationIdObj) 
                && correlationIdObj is string correlationId)
            {
                transformCtx.ProxyRequest.Headers.TryAddWithoutValidation(header, correlationId);
            }
            else if (transformCtx.HttpContext.Request.Headers.TryGetValue(header, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                transformCtx.ProxyRequest.Headers.TryAddWithoutValidation(header, existing.ToString());
            }

            await Task.CompletedTask;
        });
    });

var app = builder.Build();

app.UseCorrelationId();

app.MapGet("/health", () => Results.Ok(new { service = "gateway", status = "healthy" }));

app.MapReverseProxy();

app.Run();
