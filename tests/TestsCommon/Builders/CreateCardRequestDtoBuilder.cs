using Bogus;
using CardWebApi.Controllers.Dto;

namespace TestsCommon.Builders;

public class CreateCardRequestDtoBuilder
{
    private readonly Faker<CreateCardRequestDto> _faker;

    public CreateCardRequestDtoBuilder()
    {
        _faker = new Faker<CreateCardRequestDto>("pt_BR")
            .RuleFor(x => x.ProposalId, f => f.Random.Guid())
            .RuleFor(x => x.Limit, f => f.Finance.Amount(100, 10000, 2))
            .RuleFor(x => x.NumberOfCards, f => f.Random.Int(1, 2));
    }

    public CreateCardRequestDto Build()
    {
        return _faker.Generate();
    }

    public CreateCardRequestDtoBuilder WithProposalId(Guid proposalId)
    {
        _faker.RuleFor(x => x.ProposalId, _ => proposalId);
        return this;
    }

    public CreateCardRequestDtoBuilder WithLimit(decimal limit)
    {
        _faker.RuleFor(x => x.Limit, _ => limit);
        return this;
    }

    public CreateCardRequestDtoBuilder WithNumberOfCards(int numberOfCards)
    {
        _faker.RuleFor(x => x.NumberOfCards, _ => numberOfCards);
        return this;
    }
}

