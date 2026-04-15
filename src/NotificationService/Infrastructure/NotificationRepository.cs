using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public interface INotificationRepository
{
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
}

public class EfNotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;
    public EfNotificationRepository(NotificationDbContext db) => _db = db;

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => _db.Notifications.AnyAsync(n => n.EventId == eventId, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);
    }
}
