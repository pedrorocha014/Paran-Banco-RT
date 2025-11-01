using Application.Abstractions;
using Application.Extensions;
using CustomerWebApi.Controllers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CustomerWebApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ICreateCustomerUseCase createCustomerUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCustomerResponseDto>> Create([FromBody] CreateCustomerRequestDto request, CancellationToken cancellationToken)
    {
        var result = await createCustomerUseCase.ExecuteAsync(request.Name, cancellationToken);
        
        if (result.IsFailed)
        {
            return UnprocessableEntity(result.ToValidationProblemDetails());
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value }, new CreateCustomerResponseDto { Id = result.Value });
    }
}


