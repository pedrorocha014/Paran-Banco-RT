namespace Application.Abstractions;

public interface ICreateCardUseCase
{
    Task<CreateCardResult> ExecuteAsync(Guid proposalId, decimal limit, CancellationToken cancellationToken = default);
}

public record CreateCardResult(Guid CardId, Guid ProposalId, decimal Limit);

