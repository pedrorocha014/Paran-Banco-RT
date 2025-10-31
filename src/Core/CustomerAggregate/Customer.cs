namespace Core.CustomerAggregate;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Proposal> Proposals { get; set; } = [];
    public ICollection<Card> Cards { get; set; } = [];
}


