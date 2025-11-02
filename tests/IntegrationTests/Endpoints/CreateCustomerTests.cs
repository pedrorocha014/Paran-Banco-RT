using Core.CustomerAggregate;
using CustomerWebApi.Controllers.Dto;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using TestsCommon.Builders;

namespace IntegrationTests.Endpoints;

// IMPORTANTE: Em um mundo real eu teria usado WireMock para simular a chamada feita para consultar o score aqui no teste de integração,
//             para simplificar essa parte eu deixar alguns valores fixos de customer que devem receber certos scores.

[Collection(nameof(SharedTestCollection))]
public class CreateCustomerTests
{
    private readonly SharedTestFixture _fixture;

    public CreateCustomerTests(SharedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCustomer_WhenScoreIsGreathThan500_ShouldCompleteProposalAndCreateTwoCards()
    {
        // Arrange
        var request = new CreateCustomerRequestDtoBuilder()
            .WithName("Pedro")
            .Build();

        // Act
        var response = await _fixture.CustomerApiClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        await Task.Delay(1000);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerResponse = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        customerResponse.Should().NotBeNull();
        customerResponse!.Id.Should().NotBeEmpty();

        var proposal = await _fixture.DbContext.Proposals
            .Include(p => p.Cards)
            .FirstOrDefaultAsync(p => p.CustomerId == customerResponse.Id);

        proposal.Should().NotBeNull();
        proposal!.Status.Should().Be(ProposalStatus.Completed);
        proposal.Cards.Should().HaveCount(2);
        proposal.Cards.Should().OnlyContain(card => card.Limit == 5000.00m);
    }

    [Fact]
    public async Task CreateCustomer_WhenScoreIsGreathThan100ButLessThan500_ShouldCompleteProposalAndCreateOneCards()
    {
        // Arrange
        var request = new CreateCustomerRequestDtoBuilder()
            .WithName("João")
            .Build();

        // Act
        var response = await _fixture.CustomerApiClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        await Task.Delay(1000);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerResponse = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        customerResponse.Should().NotBeNull();
        customerResponse!.Id.Should().NotBeEmpty();

        var proposal = await _fixture.DbContext.Proposals
            .Include(p => p.Cards)
            .FirstOrDefaultAsync(p => p.CustomerId == customerResponse.Id);

        proposal.Should().NotBeNull();
        proposal!.Status.Should().Be(ProposalStatus.Completed);
        proposal.Cards.Should().HaveCount(1);
        proposal.Cards.First().Limit.Should().Be(1000.00m);
    }

    [Fact]
    public async Task CreateCustomer_WhenScoreIsLessThan100_ShouldDeniedProposalAndCreateNoCards()
    {
        // Arrange
        var request = new CreateCustomerRequestDtoBuilder()
            .WithName("Ricardo")
            .Build();

        // Act
        var response = await _fixture.CustomerApiClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        await Task.Delay(1000);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerResponse = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        customerResponse.Should().NotBeNull();
        customerResponse!.Id.Should().NotBeEmpty();

        var proposal = await _fixture.DbContext.Proposals
            .Include(p => p.Cards)
            .FirstOrDefaultAsync(p => p.CustomerId == customerResponse.Id);

        proposal.Should().NotBeNull();
        proposal!.Status.Should().Be(ProposalStatus.Denied);
        proposal.Cards.Should().HaveCount(0);
    }
}