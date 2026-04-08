using Microsoft.Extensions.Logging;
using UsersService.Domain;

namespace UsersService.Application;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

public record UserDto(Guid Id, string DisplayName, string Email, DateTime CreatedAt);
public record CreateUserCommand(string DisplayName, string Email);

public static class UserMapper
{
    public static UserDto ToDto(User u) => new(u.Id, u.DisplayName, u.Email, u.CreatedAt);
}

public class UserService
{
    private readonly IUserRepository _repo;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repo, ILogger<UserService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<UserDto> CreateAsync(CreateUserCommand cmd, CancellationToken ct = default)
    {
        if (await _repo.ExistsByEmailAsync(cmd.Email, ct))
            throw new InvalidOperationException($"Email '{cmd.Email}' is already registered.");

        var user = new User(cmd.DisplayName, cmd.Email);
        await _repo.AddAsync(user, ct);

        _logger.LogInformation("User {Id} '{Name}' created", user.Id, user.DisplayName);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(id, ct);
        return user is null ? null : UserMapper.ToDto(user);
    }
}
