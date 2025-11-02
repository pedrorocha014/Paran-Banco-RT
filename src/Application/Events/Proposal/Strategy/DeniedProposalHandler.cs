using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Messaging;
using Core.Messaging.Contracts;
using MassTransit;

namespace Application.Events.Proposal.Strategy;

public class DeniedProposalHandler(ISendEndpointProvider sendEndpointProvider) : IProposalStatusHandler
{
    public ProposalStatus Status => ProposalStatus.Denied;

    public async Task HandleAsync(ProposalCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var deniedEndpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{MessagingConstants.ProposalDeniedQueue}"));

        await deniedEndpoint.Send(
            new ProposalDenied(@event.Proposal.Id, @event.Proposal.CustomerId, DateTime.UtcNow),
            cancellationToken);
    }
}

