namespace CardWebApi.Controllers.Dto;

public class CreateCardRequestDto
{
    public Guid ProposalId { get; set; }
    public decimal Limit { get; set; }
}

public class CreateCardResponseDto
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public decimal Limit { get; set; }
}

