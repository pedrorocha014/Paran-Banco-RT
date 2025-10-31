using Application.Abstractions;
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
        try
        {
            var result = await createCardUseCase.ExecuteAsync(request.ProposalId, request.Limit, cancellationToken);
            return CreatedAtAction(nameof(Create), new { id = result.CardId }, new CreateCardResponseDto
            {
                Id = result.CardId,
                ProposalId = result.ProposalId,
                Limit = result.Limit
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

