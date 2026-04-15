using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Domain;
using NotificationService.Infrastructure;
using Shared.Contracts;

namespace NotificationService.Consumers;

public class CoreItemCreatedConsumer : IConsumer<CoreItemCreatedEvent>
{
    private readonly INotificationRepository         _repo;
    private readonly ILogger<CoreItemCreatedConsumer> _logger;

    public CoreItemCreatedConsumer(
        INotificationRepository          repo,
        ILogger<CoreItemCreatedConsumer> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CoreItemCreatedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "Received CoreItemCreatedEvent {EventId} for habit {CoreItemId}",
            evt.EventId, evt.CoreItemId);

        if (await _repo.ExistsByEventIdAsync(evt.EventId, context.CancellationToken))
        {
            _logger.LogWarning(
                "Duplicate event {EventId} - already processed, skipping.", evt.EventId);
            return;
        }

        var payload = JsonSerializer.Serialize(evt);

        var notification = new Notification(
            eventId:       evt.EventId,
            correlationId: evt.CorrelationId,
            coreItemId:    evt.CoreItemId,
            ownerUserId:   evt.OwnerUserId,
            summary:       evt.Summary,
            payload:       payload);

        try
        {
            await _repo.AddAsync(notification, context.CancellationToken);

            _logger.LogInformation(
                "Notification stored for event {EventId}, habit {CoreItemId}",
                evt.EventId, evt.CoreItemId);
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
        {
            _logger.LogWarning(
                "DB unique constraint hit for event {EventId} - skipping duplicate.", evt.EventId);
        }
    }
}
