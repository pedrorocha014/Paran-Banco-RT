using Core.Messaging;
using Core.Messaging.Contracts;
using MassTransit;
using System.Net.Http.Json;

namespace Worker.Consumers;

public class CustomerCreatedConsumer : IConsumer<CustomerCreated>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomerCreatedConsumer> _logger;

    public CustomerCreatedConsumer(
        IHttpClientFactory httpClientFactory,
        ILogger<CustomerCreatedConsumer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerCreated> context)
    {
        var customerId = context.Message.Id;

        try
        {
            _logger.LogInformation("Processing customer created event for customer {CustomerId}", customerId);

            var httpClient = _httpClientFactory.CreateClient("ProposalWebApi");
            var requestBody = new { customerId = customerId };
            
            var response = await httpClient.PostAsJsonAsync("/api/proposals", requestBody, context.CancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Proposal created successfully for customer {CustomerId}", customerId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(context.CancellationToken);
                _logger.LogWarning(
                    "Failed to create proposal for customer {CustomerId}. Status: {StatusCode}. Response: {ErrorContent}", 
                    customerId, response.StatusCode, errorContent);
                
                throw new HttpRequestException(
                    $"Failed to create proposal for customer {customerId}. Status: {response.StatusCode}. Response: {errorContent}");
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while creating proposal for customer {CustomerId}", customerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer created event for customer {CustomerId}", customerId);
            throw;
        }
    }
}


