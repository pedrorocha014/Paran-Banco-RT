using Application.UseCases.Customers;
using Core.CustomerAggregate;
using Core.Interfaces;
using Core.Messaging;
using Core.Messaging.Contracts;
using FluentAssertions;
using FluentResults;
using MassTransit;
using Moq;

namespace CustomerWebApiUnitTests.Application;

public class CreateCustomerUseCaseTests
{
    private readonly Mock<IGenericRepository<Customer>> _mockRepository;
    private readonly Mock<ISendEndpointProvider> _mockSendEndpointProvider;
    private readonly Mock<ISendEndpoint> _mockSendEndpoint;
    private readonly CreateCustomerUseCase _useCase;

    public CreateCustomerUseCaseTests()
    {
        _mockRepository = new Mock<IGenericRepository<Customer>>();
        _mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
        _mockSendEndpoint = new Mock<ISendEndpoint>();
        
        _mockSendEndpointProvider
            .Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
            .ReturnsAsync(_mockSendEndpoint.Object);
        
        _useCase = new CreateCustomerUseCase(_mockRepository.Object, _mockSendEndpointProvider.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAddAsyncFail_ShouldReturnFailError()
    {
        // Arrange
        var customerName = "João Silva";
        var errorMessage = "Erro ao adicionar cliente";
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(customerName, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
        
        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.IsAny<Uri>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesAsyncFail_ShouldReturnFailError()
    {
        // Arrange
        var customerName = "João Silva";
        var errorMessage = "Erro ao salvar alterações";
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(customerName, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.IsAny<Uri>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesWithSuccess_ShouldPublishToQueue_And_ReturnOk()
    {
        // Arrange
        var customerName = "João Silva";
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _useCase.ExecuteAsync(customerName, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        _mockRepository.Verify(
            x => x.AddAsync(It.Is<Customer>(c => c.Name == customerName), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.CustomerCreatedQueue))), 
            Times.Once);
        
        _mockSendEndpoint.Verify(
            x => x.Send(It.Is<CustomerCreated>(m => m.Name == customerName), It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}