using System.ComponentModel.DataAnnotations.Schema;

namespace Core.CustomerAggregate;

public class Proposal
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public ProposalStatus Status { get; set; }
    public Card? Card { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public int NumberOfCardsAllowed { get; set; }

    /// <summary>
    /// Avalia o score e atualiza o status da proposta baseado nas regras de negócio.
    /// </summary>
    /// <param name="score">Score da proposta (0-1000)</param>
    public void EvaluateScore(int score)
    {
        (Status, NumberOfCardsAllowed) = score switch
        {
            >= 0 and <= 100 => (ProposalStatus.Denied, 0),
            >= 101 and <= 500 => (ProposalStatus.Approved, 1),
            >= 501 => (ProposalStatus.Approved, 2),
            _ => (ProposalStatus.Denied, 0)
        };

        UpdatedAt = DateTime.UtcNow;
    }
}

