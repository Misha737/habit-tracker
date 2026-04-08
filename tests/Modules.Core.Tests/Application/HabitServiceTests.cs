using Microsoft.Extensions.Logging.Abstractions;
using Modules.Core.Application.Commands;
using Modules.Core.Application.Ports;
using Modules.Core.Application.Services;
using Modules.Core.Domain;
using Xunit;

namespace Modules.Core.Tests.Application;

public class InMemoryHabitRepository : IHabitRepository
{
    private readonly Dictionary<Guid, Habit> _store = new();

    public Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var h) ? h : null);

    public Task<bool> ExistsActiveByOwnerAndNameAsync(Guid ownerUserId, string name, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Any(h =>
            h.OwnerUserId == ownerUserId && h.Name == name.Trim() && h.Status == HabitStatus.Active));

    public Task AddAsync(Habit habit, CancellationToken ct = default)
    {
        _store[habit.Id] = habit;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Habit habit, CancellationToken ct = default)
    {
        _store[habit.Id] = habit;
        return Task.CompletedTask;
    }
}

public class AlwaysExistsUserClient : IUserValidationClient
{
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(true);
}

public class NeverExistsUserClient : IUserValidationClient
{
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(false);
}

public class UnavailableUserClient : IUserValidationClient
{
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        => throw new UserServiceUnavailableException("UsersService is unreachable.");
}

public class HabitServiceTests
{
    private static readonly Guid ValidOwner = Guid.NewGuid();

    private static HabitService CreateService(
        IHabitRepository? repo = null,
        IUserValidationClient? users = null)
    {
        repo  ??= new InMemoryHabitRepository();
        users ??= new AlwaysExistsUserClient();
        return new HabitService(repo, users, NullLogger<HabitService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidOwner_ReturnsHabitDto()
    {
        var svc = CreateService();
        var dto = await svc.CreateAsync(new CreateHabitCommand("Morning Run", "", 5, ValidOwner));

        Assert.Equal("Morning Run", dto.Name);
        Assert.Equal("Active", dto.Status);
        Assert.Equal(ValidOwner, dto.OwnerUserId);
    }

    [Fact]
    public async Task CreateAsync_OwnerNotFound_ThrowsOwnerNotFoundException()
    {
        var svc = CreateService(users: new NeverExistsUserClient());

        await Assert.ThrowsAsync<OwnerNotFoundException>(() =>
            svc.CreateAsync(new CreateHabitCommand("Run", "", 3, ValidOwner)));
    }

    [Fact]
    public async Task CreateAsync_UsersServiceDown_ThrowsUserServiceUnavailableException()
    {
        var svc = CreateService(users: new UnavailableUserClient());

        await Assert.ThrowsAsync<UserServiceUnavailableException>(() =>
            svc.CreateAsync(new CreateHabitCommand("Run", "", 3, ValidOwner)));
    }

    [Fact]
    public async Task CreateAsync_DuplicateActiveName_ThrowsInvalidOperationException()
    {
        var svc = CreateService();
        var cmd = new CreateHabitCommand("Morning Run", "", 5, ValidOwner);
        await svc.CreateAsync(cmd);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(cmd));
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ChangesStatus()
    {
        var svc     = CreateService();
        var created = await svc.CreateAsync(new CreateHabitCommand("Read", "", 7, ValidOwner));
        var updated = await svc.UpdateStatusAsync(new UpdateHabitStatusCommand(created.Id, HabitStatus.Paused));

        Assert.Equal("Paused", updated.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var svc = CreateService();
        Assert.Null(await svc.GetByIdAsync(Guid.NewGuid()));
    }
}
