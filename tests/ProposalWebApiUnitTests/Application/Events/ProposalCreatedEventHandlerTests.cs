using Application.Abstractions;
using Application.Events.Proposal;
using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Interfaces;
using Core.Messaging;
using Core.Messaging.Contracts;
using FluentAssertions;
using FluentResults;
using MassTransit;
using Moq;
using TestsCommon.Builders;

namespace ProposalWebApiUnitTests.Application.Events;

public class ProposalCreatedEventHandlerTests
{
    private readonly Mock<IGenericRepository<Proposal>> _mockRepository;
    private readonly Mock<IGenericRepository<Customer>> _mockCustomerRepository;
    private readonly Mock<ISendEndpointProvider> _mockSendEndpointProvider;
    private readonly Mock<ISendEndpoint> _mockDeniedEndpoint;
    private readonly Mock<ISendEndpoint> _mockApprovedEndpoint;
    private readonly Mock<ISendEndpoint> _mockCardEndpoint;
    private readonly Mock<IScoreCalculator> _mockScoreCalculator;
    private readonly ProposalCreatedEventHandler _handler;

    public ProposalCreatedEventHandlerTests()
    {
        _mockRepository = new Mock<IGenericRepository<Proposal>>();
        _mockCustomerRepository = new Mock<IGenericRepository<Customer>>();
        _mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
        _mockDeniedEndpoint = new Mock<ISendEndpoint>();
        _mockApprovedEndpoint = new Mock<ISendEndpoint>();
        _mockCardEndpoint = new Mock<ISendEndpoint>();
        _mockScoreCalculator = new Mock<IScoreCalculator>();

        _mockSendEndpointProvider
            .Setup(x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.ProposalDeniedQueue))))
            .ReturnsAsync(_mockDeniedEndpoint.Object);

        _mockSendEndpointProvider
            .Setup(x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.ProposalApprovedQueue))))
            .ReturnsAsync(_mockApprovedEndpoint.Object);

        _mockSendEndpointProvider
            .Setup(x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.CardIssueRequestedQueue))))
            .ReturnsAsync(_mockCardEndpoint.Object);

        _handler = new ProposalCreatedEventHandler(
            _mockRepository.Object,
            _mockCustomerRepository.Object,
            _mockSendEndpointProvider.Object,
            _mockScoreCalculator.Object);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesAsyncFail_ShouldReturn()
    {
        // Arrange
        var customer = new CustomerBuilder().Build();
        var proposal = new ProposalBuilder().WithCustomerId(customer.Id).Build();
        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        var score = 250;

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(customer));

        _mockScoreCalculator
            .Setup(x => x.CalculateScoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Erro ao salvar"));

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.Update(proposal), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.IsAny<Uri>()),
            Times.Never);

        _mockDeniedEndpoint.Verify(
            x => x.Send(It.IsAny<ProposalDenied>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockApprovedEndpoint.Verify(
            x => x.Send(It.IsAny<ProposalApproved>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockCardEndpoint.Verify(
            x => x.Send(It.IsAny<CardIssueRequested>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProposalDenied_ShouldPublishToProposalDeniedQueue()
    {
        // Arrange
        var customer = new CustomerBuilder().Build();
        var proposal = new ProposalBuilder().WithCustomerId(customer.Id).Build();
        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        var score = 50; // Score que resulta em Denied

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(customer));

        _mockScoreCalculator
            .Setup(x => x.CalculateScoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        proposal.Status.Should().Be(ProposalStatus.Denied);
        proposal.NumberOfCardsAllowed.Should().Be(0);

        _mockRepository.Verify(x => x.Update(proposal), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.ProposalDeniedQueue))),
            Times.Once);

        _mockDeniedEndpoint.Verify(
            x => x.Send(
                It.Is<ProposalDenied>(m => m.ProposalId == proposal.Id && m.CustomerId == proposal.CustomerId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockApprovedEndpoint.Verify(
            x => x.Send(It.IsAny<ProposalApproved>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockCardEndpoint.Verify(
            x => x.Send(It.IsAny<CardIssueRequested>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProposalApprovedWithOneCard_ShouldPublishToProposalApprovedQueueAndCardIssueRequestedQueue()
    {
        // Arrange
        var customer = new CustomerBuilder().Build();
        var proposal = new ProposalBuilder().WithCustomerId(customer.Id).Build();
        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        var score = 250; // Score que resulta em Approved com 1 cartão

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(customer));

        _mockScoreCalculator
            .Setup(x => x.CalculateScoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        proposal.Status.Should().Be(ProposalStatus.Approved);
        proposal.NumberOfCardsAllowed.Should().Be(1);

        _mockRepository.Verify(x => x.Update(proposal), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.ProposalApprovedQueue))),
            Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.CardIssueRequestedQueue))),
            Times.Once);

        _mockApprovedEndpoint.Verify(
            x => x.Send(
                It.Is<ProposalApproved>(m => m.ProposalId == proposal.Id && 
                                          m.CustomerId == proposal.CustomerId && 
                                          m.NumberOfCards == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCardEndpoint.Verify(
            x => x.Send(
                It.Is<CardIssueRequested>(m => m.ProposalId == proposal.Id && 
                                             m.CustomerId == proposal.CustomerId &&
                                             m.IdempotencyKey == $"{proposal.Id}-card-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDeniedEndpoint.Verify(
            x => x.Send(It.IsAny<ProposalDenied>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProposalApprovedWithTwoCard_ShouldPublishToProposalApprovedQueueAnd2xCardIssueRequestedQueue()
    {
        // Arrange
        var customer = new CustomerBuilder().Build();
        var proposal = new ProposalBuilder().WithCustomerId(customer.Id).Build();
        var @event = new ProposalCreatedEvent() with { Proposal = proposal };
        var score = 750; // Score que resulta em Approved com 2 cartões

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(customer));

        _mockScoreCalculator
            .Setup(x => x.CalculateScoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        _mockRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        proposal.Status.Should().Be(ProposalStatus.Approved);
        proposal.NumberOfCardsAllowed.Should().Be(2);

        _mockRepository.Verify(x => x.Update(proposal), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.ProposalApprovedQueue))),
            Times.Once);

        _mockSendEndpointProvider.Verify(
            x => x.GetSendEndpoint(It.Is<Uri>(u => u.ToString().Contains(MessagingConstants.CardIssueRequestedQueue))),
            Times.Once);

        _mockApprovedEndpoint.Verify(
            x => x.Send(
                It.Is<ProposalApproved>(m => m.ProposalId == proposal.Id && 
                                          m.CustomerId == proposal.CustomerId && 
                                          m.NumberOfCards == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCardEndpoint.Verify(
            x => x.Send(
                It.Is<CardIssueRequested>(m => m.ProposalId == proposal.Id && 
                                             m.CustomerId == proposal.CustomerId &&
                                             m.IdempotencyKey == $"{proposal.Id}-card-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCardEndpoint.Verify(
            x => x.Send(
                It.Is<CardIssueRequested>(m => m.ProposalId == proposal.Id && 
                                             m.CustomerId == proposal.CustomerId &&
                                             m.IdempotencyKey == $"{proposal.Id}-card-2"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDeniedEndpoint.Verify(
            x => x.Send(It.IsAny<ProposalDenied>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}