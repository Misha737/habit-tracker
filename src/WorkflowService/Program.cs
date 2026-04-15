using Microsoft.EntityFrameworkCore;
using WorkflowService.Application;
using WorkflowService.Domain;
using WorkflowService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connStr = builder.Configuration.GetConnectionString("WorkflowDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:WorkflowDb");

builder.Services.AddDbContext<WorkflowDbContext>(opts => opts.UseNpgsql(connStr));
builder.Services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
builder.Services.AddScoped<IHabitJoiningRepository, EfHabitJoiningRepository>();

var coreApiUrl = builder.Configuration["Services:CoreApiUrl"]
    ?? throw new InvalidOperationException("Missing Services:CoreApiUrl");
var usersServiceUrl = builder.Configuration["Services:UsersServiceUrl"]
    ?? throw new InvalidOperationException("Missing Services:UsersServiceUrl");
var notificationServiceUrl = builder.Configuration["Services:NotificationServiceUrl"]
    ?? throw new InvalidOperationException("Missing Services:NotificationServiceUrl");

builder.Services.AddHttpClient<SagaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<SagaService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Database.Migrate();

app.MapGet("/health", () => Results.Ok(new { service = "workflow", status = "healthy" }));

app.MapPost("/workflows/join-habit", async (
    JoinHabitRequest req,
    SagaService svc,
    CancellationToken ct) =>
{
    try
    {
        var cmd = new StartJoinHabitWorkflowCommand(req.UserId, req.HabitId);
        var workflow = await svc.StartJoinHabitAsync(cmd, ct);

        var statusCode = workflow.State == WorkflowState.Completed ? 201 : 200;
        return Results.StatusCode(statusCode, new
        {
            workflowId = workflow.WorkflowId,
            state = workflow.State.ToString(),
            joiningId = workflow.JoiningId
        });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500, new { error = ex.Message });
    }
});

app.MapGet("/workflows/{id:guid}", async (Guid id, IWorkflowRepository repo, CancellationToken ct) =>
{
    var workflow = await repo.GetByIdAsync(id, ct);
    if (workflow == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        workflowId = workflow.WorkflowId,
        type = workflow.Type.ToString(),
        state = workflow.State.ToString(),
        userId = workflow.UserId,
        habitId = workflow.HabitId,
        joiningId = workflow.JoiningId,
        createdAt = workflow.CreatedAt,
        updatedAt = workflow.UpdatedAt,
        lastError = workflow.LastError
    });
});

app.MapGet("/joinings/{id:guid}", async (Guid id, IHabitJoiningRepository repo, CancellationToken ct) =>
{
    var joining = await repo.GetByIdAsync(id, ct);
    if (joining == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        id = joining.Id,
        userId = joining.UserId,
        habitId = joining.HabitId,
        status = joining.Status.ToString(),
        createdAt = joining.CreatedAt,
        cancelledAt = joining.CancelledAt
    });
});

app.Run();

record JoinHabitRequest(Guid UserId, Guid HabitId);

public partial class Program { }
