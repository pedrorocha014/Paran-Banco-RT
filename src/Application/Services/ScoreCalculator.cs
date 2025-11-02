using Application.Abstractions;

namespace Application.Services;

public class ScoreCalculator : IScoreCalculator
{
    public async Task<int> CalculateScoreAsync(string customerName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return customerName switch
        {
            "Pedro" => 1000,
            "João" => 300,
            "Ricardo" => 50,
            _ => Random.Shared.Next(0, 1001)
        };
    }
}

