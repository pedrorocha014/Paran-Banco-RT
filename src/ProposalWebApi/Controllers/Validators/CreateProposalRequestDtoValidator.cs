using ProposalWebApi.Controllers.Dto;
using FluentValidation;

namespace ProposalWebApi.Controllers.Validators;

public class CreateProposalRequestDtoValidator : AbstractValidator<CreateProposalRequestDto>
{
    public CreateProposalRequestDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required.");
    }
}

