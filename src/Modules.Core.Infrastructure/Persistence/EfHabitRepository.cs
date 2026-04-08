using Microsoft.EntityFrameworkCore;
using Modules.Core.Application.Ports;
using Modules.Core.Domain;

namespace Modules.Core.Infrastructure.Persistence;

public class EfHabitRepository : IHabitRepository
{
    private readonly HabitDbContext _db;
    public EfHabitRepository(HabitDbContext db) => _db = db;

    public Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Habits.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<bool> ExistsActiveByOwnerAndNameAsync(Guid ownerUserId, string name, CancellationToken ct = default)
        => _db.Habits.AnyAsync(
            h => h.OwnerUserId == ownerUserId
              && h.Name == name.Trim()
              && h.Status == HabitStatus.Active, ct);

    public async Task AddAsync(Habit habit, CancellationToken ct = default)
    {
        await _db.Habits.AddAsync(habit, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Habit habit, CancellationToken ct = default)
    {
        _db.Habits.Update(habit);
        await _db.SaveChangesAsync(ct);
    }
}
