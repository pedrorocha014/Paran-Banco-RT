namespace Application.Abstractions;

public interface IScoreCalculator
{
    Task<int> CalculateScoreAsync(CancellationToken cancellationToken = default);
}

