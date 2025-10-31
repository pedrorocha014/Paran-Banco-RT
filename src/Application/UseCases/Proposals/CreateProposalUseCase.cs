using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;
using FluentResults;
using Core.Shared.Events;
using Core.CustomerAggregate.Events;

namespace Application.UseCases.Proposals;

public class CreateProposalUseCase(
    IGenericRepository<Proposal> repository,
    IBackgroundTaskQueue<ProposalCreatedEvent> backgroundTaskQueue
    ) : ICreateProposalUseCase
{
    public async Task<Result<CreateProposalResult>> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default)
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

        var addResult = await repository.AddAsync(proposal, cancellationToken);
        if (addResult.IsFailed)
        {
            return Result.Fail(addResult.Errors);
        }

        var saveResult = await repository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailed)
        {
            return Result.Fail(saveResult.Errors);
        }

        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        await backgroundTaskQueue.QueueTaskAsync(@event, cancellationToken);

        return Result.Ok(new CreateProposalResult(proposal.Id, proposal.CustomerId, proposal.Status));
    }
}

