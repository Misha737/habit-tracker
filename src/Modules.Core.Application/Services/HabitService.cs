using Microsoft.Extensions.Logging;
using Modules.Core.Application.Commands;
using Modules.Core.Application.Dto;
using Modules.Core.Application.Ports;
using Modules.Core.Domain;

namespace Modules.Core.Application.Services;

public class HabitService
{
    private readonly IHabitRepository _repository;
    private readonly IUserValidationClient _users;
    private readonly ILogger<HabitService> _logger;

    public HabitService(
        IHabitRepository repository,
        IUserValidationClient users,
        ILogger<HabitService> logger)
    {
        _repository = repository;
        _users = users;
        _logger = logger;
    }

    public async Task<HabitDto> CreateAsync(CreateHabitCommand cmd, CancellationToken ct = default)
    {
        bool userExists = await _users.UserExistsAsync(cmd.OwnerUserId, ct);
        if (!userExists)
            throw new OwnerNotFoundException($"User '{cmd.OwnerUserId}' not found.");

        bool exists = await _repository.ExistsActiveByOwnerAndNameAsync(cmd.OwnerUserId, cmd.Name, ct);
        if (exists)
            throw new InvalidOperationException(
                $"An active habit named '{cmd.Name}' already exists for this user.");

        var habit = new Habit(cmd.Name, cmd.Description, cmd.FrequencyPerWeek, cmd.OwnerUserId);
        await _repository.AddAsync(habit, ct);

        _logger.LogInformation("Habit {HabitId} '{Name}' created for owner {OwnerId}",
            habit.Id, habit.Name, habit.OwnerUserId);

        return HabitMapper.ToDto(habit);
    }

    public async Task<HabitDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var habit = await _repository.GetByIdAsync(id, ct);
        return habit is null ? null : HabitMapper.ToDto(habit);
    }

    public async Task<HabitDto> UpdateStatusAsync(UpdateHabitStatusCommand cmd, CancellationToken ct = default)
    {
        var habit = await _repository.GetByIdAsync(cmd.HabitId, ct)
            ?? throw new KeyNotFoundException($"Habit {cmd.HabitId} not found.");

        habit.ChangeStatus(cmd.NewStatus);
        await _repository.UpdateAsync(habit, ct);

        _logger.LogInformation("Habit {HabitId} status changed to {Status}", habit.Id, habit.Status);
        return HabitMapper.ToDto(habit);
    }

    public async Task<HabitDto> UpdateDetailsAsync(UpdateHabitDetailsCommand cmd, CancellationToken ct = default)
    {
        var habit = await _repository.GetByIdAsync(cmd.HabitId, ct)
            ?? throw new KeyNotFoundException($"Habit {cmd.HabitId} not found.");

        habit.UpdateDetails(cmd.Name, cmd.Description, cmd.FrequencyPerWeek);
        await _repository.UpdateAsync(habit, ct);
        return HabitMapper.ToDto(habit);
    }
}

public class OwnerNotFoundException : Exception
{
    public OwnerNotFoundException(string message) : base(message) { }
}
