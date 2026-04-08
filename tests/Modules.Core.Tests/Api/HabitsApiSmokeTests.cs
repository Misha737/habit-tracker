using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Core.Application.Ports;
using Modules.Core.Infrastructure.Persistence;
using Xunit;

namespace Modules.Core.Tests.Api;

public class FakeUserValidationClient : IUserValidationClient
{
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(userId != Guid.Empty);
}

public class HabitsApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HabitsApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HabitDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);
                services.AddDbContext<HabitDbContext>(opts =>
                    opts.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                var userClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserValidationClient));
                if (userClientDescriptor != null) services.Remove(userClientDescriptor);
                services.AddSingleton<IUserValidationClient, FakeUserValidationClient>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostCoreItem_ValidOwnerGuid_Returns201()
    {
        var payload = new
        {
            name = "Morning Run",
            description = "5km before work",
            frequencyPerWeek = 5,
            ownerUserId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/core-items", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostCoreItem_EmptyName_Returns400()
    {
        var payload = new
        {
            name = "",
            description = "desc",
            frequencyPerWeek = 3,
            ownerUserId = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/core-items", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCoreItem_EmptyOwnerGuid_Returns400()
    {
        var payload = new
        {
            name = "Run",
            description = "",
            frequencyPerWeek = 3,
            ownerUserId = Guid.Empty
        };
        var response = await _client.PostAsJsonAsync("/core-items", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCoreItem_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/core-items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
