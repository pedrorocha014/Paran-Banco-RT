using Bogus;
using CustomerWebApi.Controllers.Dto;

namespace TestsCommon.Builders;

public class CreateCustomerRequestDtoBuilder
{
    private readonly Faker<CreateCustomerRequestDto> _faker;

    public CreateCustomerRequestDtoBuilder()
    {
        _faker = new Faker<CreateCustomerRequestDto>("pt_BR")
            .RuleFor(x => x.Name, f => f.Person.FullName);
    }

    public CreateCustomerRequestDto Build()
    {
        return _faker.Generate();
    }

    public CreateCustomerRequestDtoBuilder WithName(string name)
    {
        _faker.RuleFor(x => x.Name, _ => name);
        return this;
    }
}

