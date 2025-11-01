using Bogus;
using ProposalWebApi.Controllers.Dto;

namespace TestsCommon.Builders;

public class CreateProposalRequestDtoBuilder
{
    private readonly Faker<CreateProposalRequestDto> _faker;

    public CreateProposalRequestDtoBuilder()
    {
        _faker = new Faker<CreateProposalRequestDto>("pt_BR")
            .RuleFor(x => x.CustomerId, f => f.Random.Guid());
    }

    public CreateProposalRequestDto Build()
    {
        return _faker.Generate();
    }

    public CreateProposalRequestDtoBuilder WithCustomerId(Guid customerId)
    {
        _faker.RuleFor(x => x.CustomerId, _ => customerId);
        return this;
    }
}

