namespace NotificationService.Domain;

public class Notification
{
    public Guid     EventId       { get; private set; }
    public string   CorrelationId { get; private set; } = string.Empty;
    public Guid     CoreItemId    { get; private set; }
    public Guid     OwnerUserId   { get; private set; }
    public string   Summary       { get; private set; } = string.Empty;
    public string   Payload       { get; private set; } = string.Empty;
    public DateTime CreatedAt     { get; private set; }

    private Notification() { }

    public Notification(Guid eventId, string correlationId, Guid coreItemId,
                        Guid ownerUserId, string summary, string payload)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty.");
        EventId       = eventId;
        CorrelationId = correlationId;
        CoreItemId    = coreItemId;
        OwnerUserId   = ownerUserId;
        Summary       = summary;
        Payload       = payload;
        CreatedAt     = DateTime.UtcNow;
    }
}
