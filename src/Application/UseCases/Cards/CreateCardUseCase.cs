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
    public async Task<Result<CreateCardResult>> ExecuteAsync(Guid proposalId, decimal limit, CancellationToken cancellationToken = default)
    {
        var proposalResult = await proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        
        if (proposalResult.IsFailed)
        {
            return Result.Fail(proposalResult.Errors);
        }

        var proposal = proposalResult.Value;

        if (proposal.Card != null)
        {
            return Result.Fail($"A card already exists for proposal {proposalId}.");
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

        var transactionResult = await unitOfWork.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailed)
        {
            return Result.Fail(transactionResult.Errors);
        }

        await using var transaction = transactionResult.Value;
        
        try
        {
            var addCardResult = await cardRepository.AddAsync(card, cancellationToken);
            if (addCardResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(addCardResult.Errors);
            }

            proposal.Status = ProposalStatus.Completed;
            proposal.UpdatedAt = now;
            var updateProposalResult = proposalRepository.Update(proposal);
            if (updateProposalResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(updateProposalResult.Errors);
            }

            var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(saveResult.Errors);
            }

            var commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailed)
            {
                return Result.Fail(commitResult.Errors);
            }

            return Result.Ok(new CreateCardResult(card.Id, card.ProposalId, limit));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail(ex.Message);
        }
    }
}

