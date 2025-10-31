using Core.Shared.Events;

namespace Core.CustomerAggregate.Events;

public record ProposalCreatedEvent() : IEvent
{
    public Proposal Proposal { get; set; }
}