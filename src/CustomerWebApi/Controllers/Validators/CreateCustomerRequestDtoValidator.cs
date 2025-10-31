using CustomerWebApi.Controllers.Dto;
using FluentValidation;

namespace CustomerWebApi.Controllers.Validators;

public class CreateCustomerRequestDtoValidator : AbstractValidator<CreateCustomerRequestDto>
{
    public CreateCustomerRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .NotNull()
            .WithMessage("Name is required.");
    }
}

