using Application.Abstractions;
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
        return CreatedAtAction(nameof(Create), new { id = result.ProposalId }, new CreateProposalResponseDto 
        { 
            Id = result.ProposalId,
            CustomerId = result.CustomerId,
            Status = result.Status
        });
    }
}

