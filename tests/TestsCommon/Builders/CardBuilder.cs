using Bogus;
using Core.CustomerAggregate;

namespace TestsCommon.Builders;

public class CardBuilder
{
    private readonly Faker<Card> _faker;

    public CardBuilder()
    {
        _faker = new Faker<Card>("pt_BR")
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.CustomerId, f => f.Random.Guid())
            .RuleFor(x => x.Customer, _ => null!)
            .RuleFor(x => x.ProposalId, f => f.Random.Guid())
            .RuleFor(x => x.Proposal, _ => null!)
            .RuleFor(x => x.Limite, f => f.Finance.Amount(100, 10000, 2))
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.UpdatedAt, f => f.Date.Recent());
    }

    public Card Build()
    {
        return _faker.Generate();
    }

    public CardBuilder WithId(Guid id)
    {
        _faker.RuleFor(x => x.Id, _ => id);
        return this;
    }

    public CardBuilder WithCustomerId(Guid customerId)
    {
        _faker.RuleFor(x => x.CustomerId, _ => customerId);
        return this;
    }

    public CardBuilder WithCustomer(Customer customer)
    {
        _faker.RuleFor(x => x.Customer, _ => customer);
        _faker.RuleFor(x => x.CustomerId, _ => customer.Id);
        return this;
    }

    public CardBuilder WithProposalId(Guid proposalId)
    {
        _faker.RuleFor(x => x.ProposalId, _ => proposalId);
        return this;
    }

    public CardBuilder WithProposal(Proposal proposal)
    {
        _faker.RuleFor(x => x.Proposal, _ => proposal);
        _faker.RuleFor(x => x.ProposalId, _ => proposal.Id);
        return this;
    }

    public CardBuilder WithLimite(decimal limite)
    {
        _faker.RuleFor(x => x.Limite, _ => limite);
        return this;
    }

    public CardBuilder WithCreatedAt(DateTime createdAt)
    {
        _faker.RuleFor(x => x.CreatedAt, _ => createdAt);
        return this;
    }

    public CardBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _faker.RuleFor(x => x.UpdatedAt, _ => updatedAt);
        return this;
    }
}

