using Core.CustomerAggregate;

namespace ProposalWebApi.Controllers.Dto;

public class CreateProposalRequestDto
{
    public Guid CustomerId { get; set; }
}

public class CreateProposalResponseDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public ProposalStatus Status { get; set; }
}

