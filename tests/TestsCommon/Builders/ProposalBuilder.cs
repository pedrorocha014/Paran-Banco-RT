using Bogus;
using Core.CustomerAggregate;

namespace TestsCommon.Builders;

public class ProposalBuilder
{
    private readonly Faker<Proposal> _faker;

    public ProposalBuilder()
    {
        _faker = new Faker<Proposal>("pt_BR")
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.CustomerId, f => f.Random.Guid())
            .RuleFor(x => x.Customer, _ => null!)
            .RuleFor(x => x.Status, f => f.PickRandom<ProposalStatus>())
            .RuleFor(x => x.Card, _ => null)
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.UpdatedAt, f => f.Date.Recent())
            .RuleFor(x => x.NumberOfCardsAllowed, 0);
    }

    public Proposal Build()
    {
        return _faker.Generate();
    }

    public ProposalBuilder WithId(Guid id)
    {
        _faker.RuleFor(x => x.Id, _ => id);
        return this;
    }

    public ProposalBuilder WithCustomerId(Guid customerId)
    {
        _faker.RuleFor(x => x.CustomerId, _ => customerId);
        return this;
    }

    public ProposalBuilder WithCustomer(Customer customer)
    {
        _faker.RuleFor(x => x.Customer, _ => customer);
        _faker.RuleFor(x => x.CustomerId, _ => customer.Id);
        return this;
    }

    public ProposalBuilder WithStatus(ProposalStatus status)
    {
        _faker.RuleFor(x => x.Status, _ => status);
        return this;
    }

    public ProposalBuilder WithCard(Card? card)
    {
        _faker.RuleFor(x => x.Card, _ => card);
        return this;
    }

    public ProposalBuilder WithCreatedAt(DateTime createdAt)
    {
        _faker.RuleFor(x => x.CreatedAt, _ => createdAt);
        return this;
    }

    public ProposalBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _faker.RuleFor(x => x.UpdatedAt, _ => updatedAt);
        return this;
    }

    public ProposalBuilder WithNumberOfCardsAllowed(int numberOfCardsAllowed)
    {
        _faker.RuleFor(x => x.NumberOfCardsAllowed, _ => numberOfCardsAllowed);
        return this;
    }
}

