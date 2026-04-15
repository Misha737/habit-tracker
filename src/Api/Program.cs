using Microsoft.EntityFrameworkCore;
using Modules.Core.Application.Commands;
using Modules.Core.Application.Ports;
using Modules.Core.Application.Services;
using Modules.Core.Domain;
using Modules.Core.Infrastructure;
using Modules.Core.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<HabitService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<HabitDbContext>().Database.Migrate();

app.MapGet("/health", () => Results.Ok(new { service = "core", status = "healthy" }));

app.MapPost("/core-items", async (
    CreateHabitRequest req,
    HabitService svc,
    HttpContext http,
    CancellationToken ct) =>
{
    var correlationId = http.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? "";

    try
    {
        var cmd = new CreateHabitCommand(req.Name, req.Description ?? "", req.FrequencyPerWeek, req.OwnerUserId);
        var dto = await svc.CreateAsync(cmd, correlationId, ct);
        return Results.Created($"/core-items/{dto.Id}", dto);
    }
    catch (DomainException ex)                 { return Results.BadRequest(new { error = ex.Message }); }
    catch (OwnerNotFoundException ex)          { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex)       { return Results.Conflict(new { error = ex.Message }); }
    catch (UserServiceUnavailableException ex) { return Results.Json(new { error = ex.Message }, statusCode: 503); }
});

app.MapGet("/core-items/{id:guid}", async (Guid id, HabitService svc, CancellationToken ct) =>
{
    var dto = await svc.GetByIdAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapMethods("/core-items/{id:guid}/status", ["PATCH"],
    async (Guid id, UpdateStatusRequest req, HabitService svc, CancellationToken ct) =>
{
    if (!Enum.TryParse<HabitStatus>(req.Status, ignoreCase: true, out var status))
        return Results.BadRequest(new { error = "Valid values: Active, Paused, Archived." });
    try
    {
        var dto = await svc.UpdateStatusAsync(new UpdateHabitStatusCommand(id, status), ct);
        return Results.Ok(dto);
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (DomainException ex)   { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapMethods("/core-items/{id:guid}", ["PATCH"],
    async (Guid id, UpdateDetailsRequest req, HabitService svc, CancellationToken ct) =>
{
    try
    {
        var dto = await svc.UpdateDetailsAsync(
            new UpdateHabitDetailsCommand(id, req.Name, req.Description ?? "", req.FrequencyPerWeek), ct);
        return Results.Ok(dto);
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (DomainException ex)   { return Results.BadRequest(new { error = ex.Message }); }
});

app.Run();

record CreateHabitRequest(string Name, string? Description, int FrequencyPerWeek, Guid OwnerUserId);
record UpdateStatusRequest(string Status);
record UpdateDetailsRequest(string Name, string? Description, int FrequencyPerWeek);

public partial class Program { }
