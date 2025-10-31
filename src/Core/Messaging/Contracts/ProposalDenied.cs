namespace Core.Messaging.Contracts;

public record ProposalDenied(Guid ProposalId, Guid CustomerId, DateTime DeniedAt);

