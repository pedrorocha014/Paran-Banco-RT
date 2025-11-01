using Bogus;
using Core.CustomerAggregate;

namespace TestsCommon.Builders;

public class CustomerBuilder
{
    private readonly Faker<Customer> _faker;

    public CustomerBuilder()
    {
        _faker = new Faker<Customer>("pt_BR")
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.Person.FullName)
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.UpdatedAt, f => f.Date.Recent())
            .RuleFor(x => x.Proposals, _ => [])
            .RuleFor(x => x.Cards, _ => []);
    }

    public Customer Build()
    {
        return _faker.Generate();
    }

    public CustomerBuilder WithId(Guid id)
    {
        _faker.RuleFor(x => x.Id, _ => id);
        return this;
    }

    public CustomerBuilder WithName(string name)
    {
        _faker.RuleFor(x => x.Name, _ => name);
        return this;
    }

    public CustomerBuilder WithCreatedAt(DateTime createdAt)
    {
        _faker.RuleFor(x => x.CreatedAt, _ => createdAt);
        return this;
    }

    public CustomerBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _faker.RuleFor(x => x.UpdatedAt, _ => updatedAt);
        return this;
    }

    public CustomerBuilder WithProposals(ICollection<Proposal> proposals)
    {
        _faker.RuleFor(x => x.Proposals, _ => proposals);
        return this;
    }

    public CustomerBuilder WithCards(ICollection<Card> cards)
    {
        _faker.RuleFor(x => x.Cards, _ => cards);
        return this;
    }
}

