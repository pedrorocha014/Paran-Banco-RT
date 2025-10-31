using Core.CustomerAggregate;

namespace Application.Abstractions;

public interface ICreateProposalUseCase
{
    Task<CreateProposalResult> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public record CreateProposalResult(Guid ProposalId, Guid CustomerId, ProposalStatus Status);

