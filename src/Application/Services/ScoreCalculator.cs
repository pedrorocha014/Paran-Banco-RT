using Application.Abstractions;

namespace Application.Services;

public class ScoreCalculator : IScoreCalculator
{
    public async Task<int> CalculateScoreAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return Random.Shared.Next(0, 1001);
    }
}

