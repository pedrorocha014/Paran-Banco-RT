using Application.Abstractions;
using Application.UseCases.Cards;
using Core.CustomerAggregate;
using Core.Interfaces;
using FluentAssertions;
using FluentResults;
using Moq;
using TestsCommon.Builders;

namespace CardWebApiUnitTests.Application;

public class CreateCardUseCaseTests
{
    private readonly Mock<IGenericRepository<Card>> _mockCardRepository;
    private readonly Mock<IGenericRepository<Proposal>> _mockProposalRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDatabaseTransaction> _mockTransaction;
    private readonly CreateCardUseCase _useCase;

    public CreateCardUseCaseTests()
    {
        _mockCardRepository = new Mock<IGenericRepository<Card>>();
        _mockProposalRepository = new Mock<IGenericRepository<Proposal>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTransaction = new Mock<IDatabaseTransaction>();
        
        _useCase = new CreateCardUseCase(
            _mockCardRepository.Object,
            _mockProposalRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProposalNotFound_ShouldReturnFailError()
    {
        // Arrange
        var proposalId = Guid.NewGuid();
        var limit = 1000m;
        var errorMessage = "Proposta não encontrada";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<Proposal>(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockProposalRepository.Verify(
            x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockUnitOfWork.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProposalAlreadyHasCard_ShouldReturnErrorReason()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var card = new CardBuilder().WithProposal(proposal).Build();
        proposal.Card = card;
        var proposalId = proposal.Id;
        var limit = 1000m;
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == $"A card already exists for proposal {proposalId}.");
        
        _mockProposalRepository.Verify(
            x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockUnitOfWork.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBeginTransactionAsyncFails_ShouldReturnError()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        var errorMessage = "Erro ao iniciar transação";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IDatabaseTransaction>(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockUnitOfWork.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockCardRepository.Verify(
            x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AddAsyncFails_ShouldRollbackAndReturnError()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        var errorMessage = "Erro ao adicionar cartão";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IDatabaseTransaction>(_mockTransaction.Object));
        
        _mockTransaction
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockCardRepository
            .Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockCardRepository.Verify(
            x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockProposalRepository.Verify(
            x => x.Update(It.IsAny<Proposal>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUpdateProposalFails_ShouldRollbackAndReturnError()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        var errorMessage = "Erro ao atualizar proposta";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IDatabaseTransaction>(_mockTransaction.Object));
        
        _mockTransaction
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockCardRepository
            .Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockProposalRepository
            .Setup(x => x.Update(It.IsAny<Proposal>()))
            .Returns(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockCardRepository.Verify(
            x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockProposalRepository.Verify(
            x => x.Update(It.Is<Proposal>(p => p.Status == ProposalStatus.Completed)), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesAsyncFails_ShouldRollbackAndReturnError()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        var errorMessage = "Erro ao salvar alterações";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IDatabaseTransaction>(_mockTransaction.Object));
        
        _mockCardRepository
            .Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockProposalRepository
            .Setup(x => x.Update(It.IsAny<Proposal>()))
            .Returns(Result.Ok());
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));
        
        _mockTransaction
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionCommitFails_ShouldRollbackAndReturnError()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        var errorMessage = "Erro ao fazer commit";
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IDatabaseTransaction>(_mockTransaction.Object));
        
        _mockCardRepository
            .Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockProposalRepository
            .Setup(x => x.Update(It.IsAny<Proposal>()))
            .Returns(Result.Ok());
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockTransaction
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == errorMessage);
        
        _mockTransaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionCommitWithSuccess_ShouldReturnOk()
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();
        var proposalId = proposal.Id;
        var limit = 1000m;
        
        _mockProposalRepository
            .Setup(x => x.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(proposal));
        
        _mockUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IDatabaseTransaction>(_mockTransaction.Object));
        
        _mockCardRepository
            .Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockProposalRepository
            .Setup(x => x.Update(It.IsAny<Proposal>()))
            .Returns(Result.Ok());
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        
        _mockTransaction
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _useCase.ExecuteAsync(proposalId, limit, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ProposalId.Should().Be(proposalId);
        result.Value.Limit.Should().Be(limit);
        
        _mockCardRepository.Verify(
            x => x.AddAsync(It.Is<Card>(c => c.ProposalId == proposalId && c.Limite == limit), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockProposalRepository.Verify(
            x => x.Update(It.Is<Proposal>(p => p.Status == ProposalStatus.Completed)), 
            Times.Once);
        
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _mockTransaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }
}