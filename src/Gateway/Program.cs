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

            if (!transformCtx.HttpContext.Request.Headers.TryGetValue(header, out var existing)
                || string.IsNullOrWhiteSpace(existing))
            {
                var id = Guid.NewGuid().ToString();
                transformCtx.ProxyRequest.Headers.TryAddWithoutValidation(header, id);
                transformCtx.HttpContext.Response.Headers.TryAdd(header, id);
            }
            else
            {
                transformCtx.ProxyRequest.Headers.TryAddWithoutValidation(header, existing.ToString());
            }

            await Task.CompletedTask;
        });
    });

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { service = "gateway", status = "healthy" }));

app.MapReverseProxy();

app.Run();
