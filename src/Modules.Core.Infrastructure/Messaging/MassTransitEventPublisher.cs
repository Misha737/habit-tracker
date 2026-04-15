using MassTransit;
using Modules.Core.Application.Ports;

namespace Modules.Core.Infrastructure.Messaging;

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
        => _publishEndpoint.Publish(@event, ct);
}
