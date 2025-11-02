using Application.Abstractions;
using Application.Extensions;
using CardWebApi.Controllers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CardWebApi.Controllers;

[ApiController]
[Route("api/cards")]
public class CardsController(ICreateCardUseCase createCardUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCardResponseDto>> Create([FromBody] CreateCardRequestDto request, CancellationToken cancellationToken)
    {
        var result = await createCardUseCase
            .ExecuteAsync(request.ProposalId, request.Limit, request.NumberOfCards, cancellationToken);
        
        if (result.IsFailed)
        {
            return UnprocessableEntity(result.ToValidationProblemDetails());
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value.CardId }, new CreateCardResponseDto
        {
            Id = result.Value.CardId,
            ProposalId = result.Value.ProposalId,
            Limit = result.Value.Limit
        });
    }
}

