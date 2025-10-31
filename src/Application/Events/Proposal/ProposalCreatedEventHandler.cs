using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Interfaces;
using Core.Messaging.Contracts;
using Core.Messaging;
using Core.Shared.Events;
using MassTransit;

namespace Application.Events.Proposal;

public class ProposalCreatedEventHandler(
    IGenericRepository<Core.CustomerAggregate.Proposal> repository, 
    ISendEndpointProvider sendEndpointProvider) : IEventHandler<ProposalCreatedEvent>
{
    public async Task Handle(ProposalCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var score = await CalculateScoreAsync();

        ProposalStatus finalStatus;
        int numberOfCards = 0;

        if (score >= 0 && score <= 100)
        {
            finalStatus = ProposalStatus.Denied;
        }
        else if (score >= 101 && score <= 500)
        {
            finalStatus = ProposalStatus.Approved;
            numberOfCards = 1;
        }
        else if (score >= 501 && score <= 1000)
        {
            finalStatus = ProposalStatus.Approved;
            numberOfCards = 2;
        }
        else
        {
            finalStatus = ProposalStatus.Denied;
        }

        @event.Proposal.Status = finalStatus;
        var now = DateTime.UtcNow;
        @event.Proposal.UpdatedAt = now;

        repository.Update(@event.Proposal);
        var rows = await repository.SaveChangesAsync(cancellationToken);

        if (rows > 0)
        {
            if (finalStatus == ProposalStatus.Denied)
            {
                // Publicar proposal.denied
                var deniedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.ProposalDeniedQueue}"));
                await deniedEndpoint.Send(new ProposalDenied(@event.Proposal.Id, @event.Proposal.CustomerId, now), cancellationToken);
            }
            else if (finalStatus == ProposalStatus.Approved)
            {
                // Publicar proposal.approved
                var approvedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.ProposalApprovedQueue}"));
                await approvedEndpoint.Send(new ProposalApproved(@event.Proposal.Id, @event.Proposal.CustomerId, numberOfCards, now), cancellationToken);

                // Publicar card.issue.requested para cada cartão
                var cardEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.CardIssueRequestedQueue}"));
                for (int i = 0; i < numberOfCards; i++)
                {
                    var idempotencyKey = $"{@event.Proposal.Id}-card-{i + 1}";
                    await cardEndpoint.Send(new CardIssueRequested(@event.Proposal.Id, @event.Proposal.CustomerId, idempotencyKey, now), cancellationToken);
                }
            }
        }
    }

    private static async Task<int> CalculateScoreAsync()
    {
        await Task.CompletedTask;
        return Random.Shared.Next(0, 1001);
    }
}
