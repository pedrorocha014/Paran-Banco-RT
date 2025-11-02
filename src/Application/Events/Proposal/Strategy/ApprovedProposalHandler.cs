using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Messaging;
using Core.Messaging.Contracts;
using MassTransit;

namespace Application.Events.Proposal.Strategy;

public class ApprovedProposalHandler(ISendEndpointProvider sendEndpointProvider) : IProposalStatusHandler
{
    public ProposalStatus Status => ProposalStatus.Approved;

    public async Task HandleAsync(ProposalCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var approvedEndpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{MessagingConstants.ProposalApprovedQueue}"));

        await approvedEndpoint.Send(
            new ProposalApproved(
                @event.Proposal.Id,
                @event.Proposal.CustomerId,
                @event.Proposal.NumberOfCardsAllowed,
                now),
            cancellationToken);

        var cardEndpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{MessagingConstants.CardIssueRequestedQueue}"));

        for (int i = 0; i < @event.Proposal.NumberOfCardsAllowed; i++)
        {
            var idempotencyKey = $"{@event.Proposal.Id}-card-{i + 1}";
            await cardEndpoint.Send(
                new CardIssueRequested(@event.Proposal.Id, @event.Proposal.CustomerId, idempotencyKey, now),
                cancellationToken);
        }
    }
}

