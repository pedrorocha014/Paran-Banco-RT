using Core.Messaging;
using Core.Messaging.Contracts;
using MassTransit;
using System.Net.Http.Json;

namespace Worker.Consumers;

public class ProposalApprovedConsumer : IConsumer<ProposalApproved>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProposalApprovedConsumer> _logger;

    public ProposalApprovedConsumer(
        IHttpClientFactory httpClientFactory,
        ILogger<ProposalApprovedConsumer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProposalApproved> context)
    {
        var proposalId = context.Message.ProposalId;
        var numberOfCards = context.Message.NumberOfCards;

        try
        {
            _logger.LogInformation("Processing approved proposal {ProposalId} with {NumberOfCards} card(s)", 
                proposalId, numberOfCards);

            var httpClient = _httpClientFactory.CreateClient("CardWebApi");
            var defaultLimit = numberOfCards == 2 ? 2000.00m : 1000.00m;

            var requestBody = new { ProposalId = proposalId, Limit = defaultLimit };
            
            var response = await httpClient.PostAsJsonAsync("/api/cards", requestBody, context.CancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Card created successfully for proposal {ProposalId} with limit {Limit}", 
                    proposalId, defaultLimit);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(context.CancellationToken);
                _logger.LogWarning(
                    "Failed to create card for proposal {ProposalId}. Status: {StatusCode}. Response: {ErrorContent}", 
                    proposalId, response.StatusCode, errorContent);
                
                throw new HttpRequestException(
                    $"Failed to create card for proposal {proposalId}. Status: {response.StatusCode}. Response: {errorContent}");
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while creating card for proposal {ProposalId}", proposalId);
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approved proposal {ProposalId}", proposalId);
            throw;
        }
    }
}

