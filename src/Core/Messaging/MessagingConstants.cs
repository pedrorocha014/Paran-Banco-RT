namespace Core.Messaging;

public static class MessagingConstants
{
    public const string CustomerCreatedQueue = "customer.created";
    public const string ProposalDeniedQueue = "proposal.denied";
    public const string ProposalApprovedQueue = "proposal.approved";
    public const string CardIssueRequestedQueue = "card.issue.requested";
    public const string CreatedConsumerDlq = "created.consumer.dlq";
}


