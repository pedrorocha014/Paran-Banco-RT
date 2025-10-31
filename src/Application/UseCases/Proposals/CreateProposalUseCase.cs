using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;
using MassTransit;
using Core.Messaging;
using Core.Messaging.Contracts;
using Core.Shared.Events;
using Core.CustomerAggregate.Events;

namespace Application.UseCases.Proposals;

public class CreateProposalUseCase(
    IGenericRepository<Proposal> repository,
    IBackgroundTaskQueue<ProposalCreatedEvent> backgroundTaskQueue
    ) : ICreateProposalUseCase
{
    public async Task<CreateProposalResult> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = ProposalStatus.Created,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(proposal, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        await backgroundTaskQueue.QueueTaskAsync(@event, cancellationToken);

        return new CreateProposalResult(proposal.Id, proposal.CustomerId, proposal.Status);
    }
}

