using IntegrationTests.Factories;
using IntegrationTests.Helpers;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public class SharedTestFixture : IAsyncLifetime, IDisposable
{
    private CustomerWebApiFactory? _customerApiFactory;
    private ProposalWebApiFactory? _proposalApiFactory;
    private CardWebApiFactory? _cardApiFactory;
    private WorkerHostFactory? _workerHostFactory;

    public HttpClient CustomerApiClient { get; private set; } = null!;
    public HttpClient ProposalApiClient { get; private set; } = null!;
    public HttpClient CardApiClient { get; private set; } = null!;
    
    public ApplicationDbContext DbContext { get; private set; } = null!;
    
    private IServiceScope? _customerApiScope;
    private IServiceScope? _proposalApiScope;
    private IServiceScope? _cardApiScope;

    public async Task InitializeAsync()
    {
        await TestContainersHelper.InitializeAsync();

        var postgresConnectionString = PostgresHelper.ConnectionString;
        var (rabbitHost, rabbitPort) = TestContainersHelper.GetRabbitMqConnectionInfo();
        var rabbitMqConnectionString = $"amqp://{rabbitHost}:{rabbitPort}/";

        _customerApiFactory = new CustomerWebApiFactory(postgresConnectionString, rabbitMqConnectionString);
        _proposalApiFactory = new ProposalWebApiFactory(postgresConnectionString, rabbitMqConnectionString);
        _cardApiFactory = new CardWebApiFactory(postgresConnectionString, rabbitMqConnectionString);

        CustomerApiClient = _customerApiFactory.CreateClient();
        ProposalApiClient = _proposalApiFactory.CreateClient();
        CardApiClient = _cardApiFactory.CreateClient();

        _customerApiScope = _customerApiFactory.Services.CreateScope();
        DbContext = _customerApiScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        PostgresHelper.SetContext(_customerApiScope);

        var customerApiBaseUrl = CustomerApiClient.BaseAddress?.ToString() ?? "http://localhost";
        var proposalApiBaseUrl = ProposalApiClient.BaseAddress?.ToString() ?? "http://localhost";
        var cardApiBaseUrl = CardApiClient.BaseAddress?.ToString() ?? "http://localhost";

        proposalApiBaseUrl = proposalApiBaseUrl.TrimEnd('/');
        cardApiBaseUrl = cardApiBaseUrl.TrimEnd('/');

        _workerHostFactory = new WorkerHostFactory(
            postgresConnectionString,
            rabbitMqConnectionString,
            proposalApiBaseUrl,
            cardApiBaseUrl);

        await _workerHostFactory.StartAsync();

        await Task.Delay(2000);
    }

    public async Task DisposeAsync()
    {
        if (_workerHostFactory != null)
        {
            await _workerHostFactory.DisposeAsync();
        }

        CustomerApiClient?.Dispose();
        ProposalApiClient?.Dispose();
        CardApiClient?.Dispose();

        _customerApiFactory?.Dispose();
        _proposalApiFactory?.Dispose();
        _cardApiFactory?.Dispose();

        _customerApiScope?.Dispose();
        _proposalApiScope?.Dispose();
        _cardApiScope?.Dispose();

        DbContext?.Dispose();

        await TestContainersHelper.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public async Task ResetDatabaseAsync()
    {
        await PostgresHelper.ResetAsync();
    }
}

[CollectionDefinition(nameof(SharedTestCollection))]
public class SharedTestCollection : ICollectionFixture<SharedTestFixture>
{
}
