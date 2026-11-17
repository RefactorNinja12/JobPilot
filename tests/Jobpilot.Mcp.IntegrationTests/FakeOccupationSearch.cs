using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for the Taxonomy API: matches labels containing the text.</summary>
internal sealed class FakeOccupationSearch : IOccupationSearch
{
    private static readonly Occupation[] _all =
    [
        Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
        Occupation.Create("71Ji_irM_rSJ", "Fullstack-utvecklare").Value,
    ];

    public Task<Result<IReadOnlyList<Occupation>>> SearchAsync(string? text, int maxResults, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<Occupation>>(
        [
            .. _all.Where(o => o.Label.Contains(text ?? "-", StringComparison.OrdinalIgnoreCase)).Take(maxResults),
        ]));
}
