using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Consumers;
using NotificationService.Infrastructure;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connStr = builder.Configuration.GetConnectionString("NotificationDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:NotificationDb");

builder.Services.AddDbContext<NotificationDbContext>(opts => opts.UseNpgsql(connStr));
builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();

var rabbitHost = builder.Configuration["RabbitMQ:Host"]     ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CoreItemCreatedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("notification-service", e =>
        {
            e.ConfigureConsumer<CoreItemCreatedConsumer>(ctx);
        });
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.Migrate();

app.MapGet("/health", () => Results.Ok(new { service = "notification", status = "healthy" }));

app.MapGet("/notifications", async (NotificationDbContext db, CancellationToken ct) =>
{
    var items = await db.Notifications
        .OrderByDescending(n => n.CreatedAt)
        .Take(50)
        .Select(n => new {
            n.EventId,
            n.CorrelationId,
            n.CoreItemId,
            n.OwnerUserId,
            n.Summary,
            n.CreatedAt
        })
        .ToListAsync(ct);

    return Results.Ok(items);
});

app.Run();
