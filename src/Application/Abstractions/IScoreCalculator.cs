namespace Application.Abstractions;

public interface IScoreCalculator
{
    Task<int> CalculateScoreAsync(string customerName, CancellationToken cancellationToken = default);
}

