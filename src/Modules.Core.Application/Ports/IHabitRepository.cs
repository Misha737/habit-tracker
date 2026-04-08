using Modules.Core.Domain;

namespace Modules.Core.Application.Ports;

public interface IHabitRepository
{
    Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsActiveByOwnerAndNameAsync(Guid ownerUserId, string name, CancellationToken ct = default);
    Task AddAsync(Habit habit, CancellationToken ct = default);
    Task UpdateAsync(Habit habit, CancellationToken ct = default);
}
