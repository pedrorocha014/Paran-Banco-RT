using Core.CustomerAggregate;
using FluentAssertions;
using TestsCommon.Builders;

namespace ProposalWebApiUnitTests.Core;

public class ProposalTests
{
    [Theory]
    [InlineData(50, ProposalStatus.Denied, 0)]
    [InlineData(101, ProposalStatus.Approved, 1)]
    [InlineData(500, ProposalStatus.Approved, 1)]
    [InlineData(501, ProposalStatus.Approved, 2)]
    [InlineData(1000, ProposalStatus.Approved, 2)]
    public void EvaluateScore_ShouldSetCorrectStatusAndNumberOfCardsAllowed(
        int score, 
        ProposalStatus expectedStatus, 
        int expectedNumberOfCardsAllowed)
    {
        // Arrange
        var proposal = new ProposalBuilder().Build();

        // Act
        proposal.EvaluateScore(score);

        // Assert
        proposal.Status.Should().Be(expectedStatus);
        proposal.NumberOfCardsAllowed.Should().Be(expectedNumberOfCardsAllowed);
        proposal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}