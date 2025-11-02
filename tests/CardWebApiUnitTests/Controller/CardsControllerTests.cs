using Application.Abstractions;
using CardWebApi.Controllers;
using CardWebApi.Controllers.Dto;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TestsCommon.Builders;

namespace CardWebApiUnitTests.Controller;

public class CardsControllerTests
{
    private readonly Mock<ICreateCardUseCase> _mockCreateCardUseCase;
    private readonly CardsController _controller;

    public CardsControllerTests()
    {
        _mockCreateCardUseCase = new Mock<ICreateCardUseCase>();
        _controller = new CardsController(_mockCreateCardUseCase.Object);
    }

    [Fact]
    public async Task Create_WhenExecuteAsyncFails_ShouldReturn422()
    {
        // Arrange
        var requestDto = new CreateCardRequestDtoBuilder().Build();
        var errorMessage = "Erro ao criar cartão";

        _mockCreateCardUseCase
            .Setup(x => x.ExecuteAsync(requestDto.ProposalId, requestDto.Limit, requestDto.NumberOfCards, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<CreateCardResult>(errorMessage));

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

        _mockCreateCardUseCase.Verify(
            x => x.ExecuteAsync(requestDto.ProposalId, requestDto.Limit, requestDto.NumberOfCards, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenExecuteAsyncWithSuccess_ShouldReturn201WithValues()
    {
        // Arrange
        var requestDto = new CreateCardRequestDtoBuilder().Build();
        var cardId = Guid.NewGuid();
        var proposalId = requestDto.ProposalId;
        var limit = requestDto.Limit;

        var createCardResult = new CreateCardResult(cardId, proposalId, limit);

        _mockCreateCardUseCase
            .Setup(x => x.ExecuteAsync(requestDto.ProposalId, requestDto.Limit, requestDto.NumberOfCards, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(createCardResult));

        // Act
        var result = await _controller.Create(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = result.Result.As<CreatedAtActionResult>();
        createdAtActionResult.StatusCode.Should().Be(201);

        createdAtActionResult.Value.Should().BeOfType<CreateCardResponseDto>();
        var responseDto = createdAtActionResult.Value.As<CreateCardResponseDto>();
        responseDto.Id.Should().Be(cardId);
        responseDto.ProposalId.Should().Be(proposalId);
        responseDto.Limit.Should().Be(limit);

        _mockCreateCardUseCase.Verify(
            x => x.ExecuteAsync(requestDto.ProposalId, requestDto.Limit, requestDto.NumberOfCards, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
