using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;
using FluentResults;

namespace Application.UseCases.Cards;

public class CreateCardUseCase(
    IGenericRepository<Card> cardRepository,
    IGenericRepository<Proposal> proposalRepository,
    IUnitOfWork unitOfWork
    ) : ICreateCardUseCase
{
    public async Task<Result<CreateCardResult>> ExecuteAsync(Guid proposalId, decimal limit, int numberOfCards, CancellationToken cancellationToken = default)
    {
        if (numberOfCards <= 0)
        {
            return Result.Fail("NumberOfCards must be greater than zero.");
        }

        var proposalResult = await proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        
        if (proposalResult.IsFailed)
        {
            return Result.Fail(proposalResult.Errors);
        }

        var proposal = proposalResult.Value;

        var now = DateTime.UtcNow;
        var transactionResult = await unitOfWork.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailed)
        {
            return Result.Fail(transactionResult.Errors);
        }

        await using var transaction = transactionResult.Value;
        
        try
        {
            List<Card> cards = [];

            for (int i = 0; i < numberOfCards; i++)
            {
                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    CustomerId = proposal.CustomerId,
                    ProposalId = proposalId,
                    Limit = limit,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                
                cards.Add(card);
            }

            var addRangeResult = await cardRepository.AddRangeAsync(cards, cancellationToken);
            if (addRangeResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(addRangeResult.Errors);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            proposal.Status = ProposalStatus.Completed;
            proposal.UpdatedAt = now;
            var updateProposalResult = proposalRepository.Update(proposal);
            if (updateProposalResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(updateProposalResult.Errors);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailed)
            {
                return Result.Fail(commitResult.Errors);
            }

            var firstCard = cards.First();
            return Result.Ok(new CreateCardResult(firstCard.Id, firstCard.ProposalId, limit));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            return Result.Fail($"Unexpected error creating {numberOfCards} card(s) for proposal {proposalId}. {ex}");
        }
    }
}

