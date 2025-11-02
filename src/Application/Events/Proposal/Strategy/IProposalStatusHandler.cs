using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;

namespace Application.Events.Proposal.Strategy;

public interface IProposalStatusHandler
{
    ProposalStatus Status { get; }
    Task HandleAsync(ProposalCreatedEvent @event, CancellationToken cancellationToken = default);
}

