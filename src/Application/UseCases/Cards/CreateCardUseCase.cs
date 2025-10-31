using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;

namespace Application.UseCases.Cards;

public class CreateCardUseCase(
    IGenericRepository<Card> cardRepository,
    IGenericRepository<Proposal> proposalRepository,
    IUnitOfWork unitOfWork
    ) : ICreateCardUseCase
{
    public async Task<CreateCardResult> ExecuteAsync(Guid proposalId, decimal limit, CancellationToken cancellationToken = default)
    {
        var proposal = await proposalRepository.GetByIdAsync(proposalId, cancellationToken) ?? 
            throw new InvalidOperationException($"Proposal with id {proposalId} not found.");

        if (proposal.Card != null)
        {
            throw new InvalidOperationException($"A card already exists for proposal {proposalId}.");
        }

        var now = DateTime.UtcNow;
        var card = new Card
        {
            Id = Guid.NewGuid(),
            CustomerId = proposal.CustomerId,
            ProposalId = proposalId,
            Limite = limit,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await cardRepository.AddAsync(card, cancellationToken);

            proposal.Status = ProposalStatus.Completed;
            proposal.UpdatedAt = now;
            proposalRepository.Update(proposal);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateCardResult(card.Id, card.ProposalId, limit);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

