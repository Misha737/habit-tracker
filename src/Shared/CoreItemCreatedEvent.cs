namespace Shared.Contracts;

public record CoreItemCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string CorrelationId { get; init; } = string.Empty;

    public Guid CoreItemId { get; init; }

    public Guid OwnerUserId { get; init; }

    public string Summary { get; init; } = string.Empty;
}
