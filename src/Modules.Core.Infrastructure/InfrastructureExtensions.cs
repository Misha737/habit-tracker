using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Core.Application.Ports;
using Modules.Core.Infrastructure.Http;
using Modules.Core.Infrastructure.Messaging;
using Modules.Core.Infrastructure.Persistence;

namespace Modules.Core.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CoreDb")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:CoreDb");

        services.AddDbContext<HabitDbContext>(opts => opts.UseNpgsql(connectionString));
        services.AddScoped<IHabitRepository, EfHabitRepository>();

        var usersBaseUrl = configuration["UsersService:BaseUrl"]
            ?? throw new InvalidOperationException("Missing UsersService:BaseUrl");

        services.AddHttpClient<IUserValidationClient, HttpUserValidationClient>(client =>
        {
            client.BaseAddress = new Uri(usersBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        var rabbitHost = configuration["RabbitMQ:Host"]     ?? "localhost";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}
