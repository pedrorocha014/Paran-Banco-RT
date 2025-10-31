using Core.CustomerAggregate;
using FluentResults;

namespace Application.Abstractions;

public interface ICreateProposalUseCase
{
    Task<Result<CreateProposalResult>> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public record CreateProposalResult(Guid ProposalId, Guid CustomerId, ProposalStatus Status);

