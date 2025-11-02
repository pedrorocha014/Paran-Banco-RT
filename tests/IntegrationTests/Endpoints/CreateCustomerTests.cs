using System.Net;
using System.Net.Http.Json;
using CustomerWebApi.Controllers.Dto;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using TestsCommon.Builders;
using Xunit;

namespace IntegrationTests.Endpoints;

[Collection(nameof(SharedTestCollection))]
public class CreateCustomerTests
{
    private readonly SharedTestFixture _fixture;

    public CreateCustomerTests(SharedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturnCreated_AndPersistCustomer()
    {
        // Arrange
        var request = new CreateCustomerRequestDtoBuilder()
            .WithName("João Silva")
            .Build();

        // Act
        var response = await _fixture.CustomerApiClient.PostAsJsonAsync("/api/customers", request);

        await Task.Delay(1000);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseDto = await response.Content.ReadFromJsonAsync<CreateCustomerResponseDto>();
        Assert.NotNull(responseDto);
        Assert.NotEqual(Guid.Empty, responseDto.Id);

        // Verify customer was persisted in database
        var customer = await PostgresHelper.Context!.Customers
            .FirstOrDefaultAsync(c => c.Id == responseDto.Id);

        Assert.NotNull(customer);
        Assert.Equal(request.Name, customer.Name);
        Assert.NotEqual(default(DateTime), customer.CreatedAt);
        Assert.NotEqual(default(DateTime), customer.UpdatedAt);
    }
}