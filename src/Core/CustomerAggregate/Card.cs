namespace Core.CustomerAggregate;

public class Card
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid ProposalId { get; set; }
    public Proposal Proposal { get; set; } = null!;
    public decimal Limit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

