using Application.Abstractions;
using Application.Events.Proposal.Strategy;
using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Interfaces;
using Core.Shared.Events;

namespace Application.Events.Proposal;

public class ProposalCreatedEventHandler(
    IGenericRepository<Core.CustomerAggregate.Proposal> repository,
    IGenericRepository<Customer> customerRepository,
    IScoreCalculator scoreCalculator,
    IEnumerable<IProposalStatusHandler> handlers) : IEventHandler<ProposalCreatedEvent>
{
    public async Task Handle(ProposalCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var customerResult = await customerRepository.GetByIdAsync(@event.Proposal.CustomerId, cancellationToken);

        var score = await scoreCalculator.CalculateScoreAsync(customerResult.Value.Name, cancellationToken); 

        @event.Proposal.EvaluateScore(score);

        repository.Update(@event.Proposal);

        var saveResult = await repository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailed)
        {
            // To Do: Adicionar uma tabela de error logs, por exemplo
            return;
        }

        var handler = handlers.FirstOrDefault(h => h.Status == @event.Proposal.Status);
        await handler!.HandleAsync(@event, cancellationToken);
    }
}
