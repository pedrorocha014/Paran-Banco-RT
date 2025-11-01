using Application.Abstractions;
using Core.CustomerAggregate;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProposalWebApi.Controllers;
using ProposalWebApi.Controllers.Dto;
using TestsCommon.Builders;

namespace ProposalWebApiUnitTests.Controller;

public class ProposalsControllerTests
{
    private readonly Mock<ICreateProposalUseCase> _mockCreateProposalUseCase;
    private readonly ProposalsController _controller;

    public ProposalsControllerTests()
    {
        _mockCreateProposalUseCase = new Mock<ICreateProposalUseCase>();
        _controller = new ProposalsController(_mockCreateProposalUseCase.Object);
    }

    [Fact]
    public async Task Create_WhenExecuteAsyncFail_ShouldReturn422WithProblemDetails()
    {
        // Arrange
        var requestDto = new CreateProposalRequestDtoBuilder().Build();
        var errorMessage = "Erro ao criar proposta";
        
        _mockCreateProposalUseCase
            .Setup(x => x.ExecuteAsync(requestDto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<CreateProposalResult>(errorMessage));

        // Act
        var result = await _controller.Create(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
        var unprocessableEntityResult = result.Result.As<UnprocessableEntityObjectResult>();
        unprocessableEntityResult.StatusCode.Should().Be(422);
        
        unprocessableEntityResult.Value.Should().BeOfType<ValidationProblemDetails>();
        var problemDetails = unprocessableEntityResult.Value.As<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey("errors");
        
        _mockCreateProposalUseCase.Verify(
            x => x.ExecuteAsync(requestDto.CustomerId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenExecuteAsyncWithSuccess_ShouldReturn201WithValues()
    {
        // Arrange
        var requestDto = new CreateProposalRequestDtoBuilder().Build();
        var proposalId = Guid.NewGuid();
        var customerId = requestDto.CustomerId;
        var status = ProposalStatus.Created;
        
        var createProposalResult = new CreateProposalResult(proposalId, customerId, status);
        
        _mockCreateProposalUseCase
            .Setup(x => x.ExecuteAsync(requestDto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(createProposalResult));

        // Act
        var result = await _controller.Create(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = result.Result.As<CreatedAtActionResult>();
        createdAtActionResult.StatusCode.Should().Be(201);
        
        createdAtActionResult.Value.Should().BeOfType<CreateProposalResponseDto>();
        var responseDto = createdAtActionResult.Value.As<CreateProposalResponseDto>();
        responseDto.Id.Should().Be(proposalId);
        responseDto.CustomerId.Should().Be(customerId);
        responseDto.Status.Should().Be(status);
        
        _mockCreateProposalUseCase.Verify(
            x => x.ExecuteAsync(requestDto.CustomerId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}