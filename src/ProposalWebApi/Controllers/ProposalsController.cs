using Application.Abstractions;
using Application.Extensions;
using ProposalWebApi.Controllers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ProposalWebApi.Controllers;

[ApiController]
[Route("api/proposals")]
public class ProposalsController(ICreateProposalUseCase createProposalUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateProposalResponseDto>> Create([FromBody] CreateProposalRequestDto request, CancellationToken cancellationToken)
    {
        var result = await createProposalUseCase.ExecuteAsync(request.CustomerId, cancellationToken);
        
        if (result.IsFailed)
        {
            return BadRequest(result.ToValidationProblemDetails());
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value.ProposalId }, new CreateProposalResponseDto 
        { 
            Id = result.Value.ProposalId,
            CustomerId = result.Value.CustomerId,
            Status = result.Value.Status
        });
    }
}

