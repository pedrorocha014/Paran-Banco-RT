using Core.Messaging;
using Core.Messaging.Contracts;
using MassTransit;
using System.Net.Http.Json;
using System.Text.Json;

namespace Worker.Consumers;

public class CustomerCreatedConsumer : IConsumer<CustomerCreated>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<CustomerCreatedConsumer> _logger;

    public CustomerCreatedConsumer(
        IHttpClientFactory httpClientFactory,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<CustomerCreatedConsumer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerCreated> context)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ProposalWebApi");
            var requestBody = new { customerId = context.Message.Id };
            
            var response = await httpClient.PostAsJsonAsync("/api/proposals", requestBody, context.CancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Proposal created successfully for customer {CustomerId}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("Failed to create proposal for customer {CustomerId}. Status: {StatusCode}. Sending to DLQ.", 
                    context.Message.Id, response.StatusCode);
                
                await SendToDlq(context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer created event for customer {CustomerId}. Sending to DLQ.", 
                context.Message.Id);
            
            await SendToDlq(context);
        }
    }

    private async Task SendToDlq(ConsumeContext<CustomerCreated> context)
    {
        var dlqEndpoint = await _sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{MessagingConstants.CreatedConsumerDlq}"));
        
        await dlqEndpoint.Send(context.Message, context.CancellationToken);
    }
}


