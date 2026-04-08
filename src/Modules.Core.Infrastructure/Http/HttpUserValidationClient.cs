using System.Net;
using Microsoft.Extensions.Logging;
using Modules.Core.Application.Ports;

namespace Modules.Core.Infrastructure.Http;

public class HttpUserValidationClient : IUserValidationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpUserValidationClient> _logger;

    public HttpUserValidationClient(HttpClient http, ILogger<HttpUserValidationClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/users/{userId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            if (response.IsSuccessStatusCode)
                return true;

            _logger.LogWarning("UsersService returned {Code} for user {Id}",
                (int)response.StatusCode, userId);
            throw new UserServiceUnavailableException(
                $"UsersService returned unexpected {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot reach UsersService for user {Id}", userId);
            throw new UserServiceUnavailableException("UsersService is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout calling UsersService for user {Id}", userId);
            throw new UserServiceUnavailableException("UsersService timed out.", ex);
        }
    }
}
