namespace Core.Messaging.Contracts;

public record ProposalApproved(Guid ProposalId, Guid CustomerId, int NumberOfCards, DateTime ApprovedAt);

