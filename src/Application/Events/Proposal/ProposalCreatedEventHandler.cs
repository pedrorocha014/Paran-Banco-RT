using Application.Abstractions;
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
    ISendEndpointProvider sendEndpointProvider,
    IScoreCalculator scoreCalculator) : IEventHandler<ProposalCreatedEvent>
{
    public async Task Handle(ProposalCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // Simula uma chamada externa para consultar o score
        var score = await scoreCalculator.CalculateScoreAsync(cancellationToken); 

        @event.Proposal.EvaluateScore(score);

        repository.Update(@event.Proposal);

        var saveResult = await repository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailed)
        {
            // To Do: Adicionar uma tabela de error logs, por exemplo
            return;
        }

        var now = DateTime.UtcNow;

        if (@event.Proposal.Status == ProposalStatus.Denied)
        {
            // Publicar proposal.denied
            var deniedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.ProposalDeniedQueue}"));
            await deniedEndpoint.Send(new ProposalDenied(@event.Proposal.Id, @event.Proposal.CustomerId, now), cancellationToken);
        }
        else if (@event.Proposal.Status == ProposalStatus.Approved)
        {
            // Publicar proposal.approved
            var approvedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.ProposalApprovedQueue}"));
            await approvedEndpoint.Send(new ProposalApproved(@event.Proposal.Id, @event.Proposal.CustomerId, @event.Proposal.NumberOfCardsAllowed, now), cancellationToken);

            // Publicar card.issue.requested para cada cartão
            var cardEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.CardIssueRequestedQueue}"));
            for (int i = 0; i < @event.Proposal.NumberOfCardsAllowed; i++)
            {
                var idempotencyKey = $"{@event.Proposal.Id}-card-{i + 1}";
                await cardEndpoint.Send(new CardIssueRequested(@event.Proposal.Id, @event.Proposal.CustomerId, idempotencyKey, now), cancellationToken);
            }
        }
    }
}
