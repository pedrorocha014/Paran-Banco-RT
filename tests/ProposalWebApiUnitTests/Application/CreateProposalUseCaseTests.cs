using Application.Abstractions;
using Application.UseCases.Proposals;
using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Interfaces;
using Core.Shared.Events;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ProposalWebApiUnitTests.Application;

public class CreateProposalUseCaseTests
{
    private readonly Mock<IGenericRepository<Proposal>> _mockRepository;
    private readonly Mock<IBackgroundTaskQueue<ProposalCreatedEvent>> _mockBackgroundTaskQueue;
    private readonly CreateProposalUseCase _useCase;

    public CreateProposalUseCaseTests()
    {
        _mockRepository = new Mock<IGenericRepository<Proposal>>();
        _mockBackgroundTaskQueue = new Mock<IBackgroundTaskQueue<ProposalCreatedEvent>>();
        
        _useCase = new CreateProposalUseCase(_mockRepository.Object, _mockBackgroundTaskQueue.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAddAsyncFail_ShouldReturnFailError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var errorMessage = "Erro ao adicionar proposta";
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(customerId, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
        
        _mockBackgroundTaskQueue.Verify(
            x => x.QueueTaskAsync(It.IsAny<ProposalCreatedEvent>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesAsyncFail_ShouldReturnFailError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var errorMessage = "Erro ao salvar alterações";
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(customerId, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockBackgroundTaskQueue.Verify(
            x => x.QueueTaskAsync(It.IsAny<ProposalCreatedEvent>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesAsyncWithSuccess_ShouldQueueTaskAsyncAndReturnOk()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _useCase.ExecuteAsync(customerId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.Status.Should().Be(ProposalStatus.Created);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.Is<Proposal>(p => p.CustomerId == customerId && p.Status == ProposalStatus.Created), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockBackgroundTaskQueue.Verify(
            x => x.QueueTaskAsync(
                It.Is<ProposalCreatedEvent>(e => e.Proposal != null && e.Proposal.CustomerId == customerId), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}