namespace Core.Messaging.Contracts;

public record CardIssueRequested(Guid ProposalId, Guid CustomerId, string IdempotencyKey, DateTime RequestedAt);

