using Application.Abstractions;
using CustomerWebApi.Controllers;
using CustomerWebApi.Controllers.Dto;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TestsCommon.Builders;

namespace CustomerWebApiUnitTests.Controller;

public class CustomersControllerTests
{
    private readonly Mock<ICreateCustomerUseCase> _mockCreateCustomerUseCase;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _mockCreateCustomerUseCase = new Mock<ICreateCustomerUseCase>();
        _controller = new CustomersController(_mockCreateCustomerUseCase.Object);
    }

    [Fact]
    public async Task Create_WhenCreateCustomerWithSuccess_ShouldReturn201WithId()
    {
        // Arrange
        var requestDto = new CreateCustomerRequestDtoBuilder().Build();
        var customerId = Guid.NewGuid();
        
        _mockCreateCustomerUseCase
            .Setup(x => x.ExecuteAsync(requestDto.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(customerId));

        // Act
        var result = await _controller.Create(requestDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = result.Result.As<CreatedAtActionResult>();
        createdAtActionResult.StatusCode.Should().Be(201);
        
        createdAtActionResult.Value.Should().BeOfType<CreateCustomerResponseDto>();
        var responseDto = createdAtActionResult.Value.As<CreateCustomerResponseDto>();
        responseDto.Id.Should().Be(customerId);
        
        _mockCreateCustomerUseCase.Verify(
            x => x.ExecuteAsync(requestDto.Name, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenCreateCustomerWithFail_ShouldReturn422WithProblemDetails()
    {
        // Arrange
        var requestDto = new CreateCustomerRequestDtoBuilder().Build();
        var errorMessage = "Erro ao criar cliente";
        
        _mockCreateCustomerUseCase
            .Setup(x => x.ExecuteAsync(requestDto.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<Guid>(errorMessage));

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
        
        _mockCreateCustomerUseCase.Verify(
            x => x.ExecuteAsync(requestDto.Name, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}