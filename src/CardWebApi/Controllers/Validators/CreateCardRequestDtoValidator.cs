using CardWebApi.Controllers.Dto;
using FluentValidation;

namespace CardWebApi.Controllers.Validators;

public class CreateCardRequestDtoValidator : AbstractValidator<CreateCardRequestDto>
{
    public CreateCardRequestDtoValidator()
    {
        RuleFor(x => x.ProposalId)
            .NotEmpty()
            .WithMessage("ProposalId is required.");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than zero.");

        RuleFor(x => x.NumberOfCards)
            .GreaterThan(0)
            .WithMessage("NumberOfCards must be greater than zero.");
    }
}

