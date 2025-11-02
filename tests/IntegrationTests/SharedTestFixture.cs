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
        var postgresConnectionString = PostgresHelper.ConnectionString;
        var rabbitMqConnectionString = $"amqp://localhost:5672/";

        _customerApiFactory = new CustomerWebApiFactory(postgresConnectionString, rabbitMqConnectionString);
        _proposalApiFactory = new ProposalWebApiFactory(postgresConnectionString, rabbitMqConnectionString);
        _cardApiFactory = new CardWebApiFactory(postgresConnectionString, rabbitMqConnectionString);

        CustomerApiClient = _customerApiFactory.CreateClient();
        ProposalApiClient = _proposalApiFactory.CreateClient();
        CardApiClient = _cardApiFactory.CreateClient();

        _customerApiScope = _customerApiFactory.Services.CreateScope();
        DbContext = _customerApiScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        PostgresHelper.SetContext(_customerApiScope);

        var customerApiBaseUrl = _customerApiFactory.Server.BaseAddress.ToString();
        var proposalApiBaseUrl = _proposalApiFactory.Server.BaseAddress.ToString();
        var cardApiBaseUrl = _cardApiFactory.Server.BaseAddress.ToString();

        proposalApiBaseUrl = proposalApiBaseUrl.TrimEnd('/');
        cardApiBaseUrl = cardApiBaseUrl.TrimEnd('/');

        // Usar os handlers do TestServer para permitir que o Worker faça chamadas HTTP
        var proposalApiHandler = _proposalApiFactory.Server.CreateHandler();
        var cardApiHandler = _cardApiFactory.Server.CreateHandler();

        _workerHostFactory = new WorkerHostFactory(
            postgresConnectionString,
            rabbitMqConnectionString,
            proposalApiBaseUrl,
            cardApiBaseUrl,
            proposalApiHandler,
            cardApiHandler);

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
