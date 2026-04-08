using Microsoft.EntityFrameworkCore;
using UsersService.Application;
using UsersService.Domain;
using UsersService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUsersInfrastructure(builder.Configuration);
builder.Services.AddScoped<UserService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<UserDbContext>().Database.Migrate();

app.MapGet("/health", () => Results.Ok(new { service = "users", status = "healthy" }));

app.MapPost("/users", async (CreateUserRequest req, UserService svc, CancellationToken ct) =>
{
    try
    {
        var dto = await svc.CreateAsync(new CreateUserCommand(req.DisplayName, req.Email), ct);
        return Results.Created($"/users/{dto.Id}", dto);
    }
    catch (UserDomainException ex)       { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
});

app.MapGet("/users/{id:guid}", async (Guid id, UserService svc, CancellationToken ct) =>
{
    var dto = await svc.GetByIdAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.Run();

record CreateUserRequest(string DisplayName, string Email);

public partial class Program { }
