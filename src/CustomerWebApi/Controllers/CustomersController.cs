using Application.Abstractions;
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
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var id = await createCustomerUseCase.ExecuteAsync(request.Name, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id }, new CreateCustomerResponseDto { Id = id });
    }
}


