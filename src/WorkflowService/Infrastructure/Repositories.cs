using Microsoft.EntityFrameworkCore;
using WorkflowService.Domain;

namespace WorkflowService.Infrastructure;

public interface IWorkflowRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(WorkflowInstance workflow, CancellationToken ct = default);
    Task UpdateAsync(WorkflowInstance workflow, CancellationToken ct = default);
}

public class EfWorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _db;
    public EfWorkflowRepository(WorkflowDbContext db) => _db = db;

    public Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.WorkflowInstances.FirstOrDefaultAsync(w => w.WorkflowId == id, ct);

    public async Task AddAsync(WorkflowInstance workflow, CancellationToken ct = default)
    {
        await _db.WorkflowInstances.AddAsync(workflow, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WorkflowInstance workflow, CancellationToken ct = default)
    {
        _db.WorkflowInstances.Update(workflow);
        await _db.SaveChangesAsync(ct);
    }
}

public interface IHabitJoiningRepository
{
    Task<HabitJoining?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(HabitJoining joining, CancellationToken ct = default);
    Task UpdateAsync(HabitJoining joining, CancellationToken ct = default);
}

public class EfHabitJoiningRepository : IHabitJoiningRepository
{
    private readonly WorkflowDbContext _db;
    public EfHabitJoiningRepository(WorkflowDbContext db) => _db = db;

    public Task<HabitJoining?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.HabitJoinings.FirstOrDefaultAsync(h => h.Id == id, ct);

    public async Task AddAsync(HabitJoining joining, CancellationToken ct = default)
    {
        await _db.HabitJoinings.AddAsync(joining, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(HabitJoining joining, CancellationToken ct = default)
    {
        _db.HabitJoinings.Update(joining);
        await _db.SaveChangesAsync(ct);
    }
}
