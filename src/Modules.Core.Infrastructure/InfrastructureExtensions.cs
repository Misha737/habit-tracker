using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Core.Application.Ports;
using Modules.Core.Infrastructure.Http;
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

        return services;
    }
}
