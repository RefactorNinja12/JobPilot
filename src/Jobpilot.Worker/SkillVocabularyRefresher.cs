using Jobpilot.Application.SkillVocabularies.Refresh;

namespace Jobpilot.Worker;

/// <summary>
/// Refreshes the skill vocabulary at start and then every <see cref="Interval"/>.
/// A failed run is logged and retried on the next tick; it never stops the Worker.
/// </summary>
internal sealed partial class SkillVocabularyRefresher(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<SkillVocabularyRefresher> logger) : BackgroundService
{
    /// <summary>The vocabulary changes seldom (last build 2025-06-17); daily is plenty.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        do
        {
            await RefreshAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<RefreshSkillVocabularyHandler>();
            var result = await handler.HandleAsync(stoppingToken);

            if (result.IsFailure)
            {
                LogFailed(result.Error.Code, result.Error.Message);
            }
            else if (result.Value.Updated)
            {
                LogUpdated(result.Value.BuiltAt, result.Value.ConceptCount);
            }
            else
            {
                LogUnchanged(result.Value.BuiltAt);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            // E.g. the database is down. Log and try again on the next tick instead of stopping the host.
            LogCrashed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Skill vocabulary updated to build {BuiltAt} ({ConceptCount} concepts)")]
    private partial void LogUpdated(DateTimeOffset builtAt, int conceptCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skill vocabulary unchanged (build {BuiltAt})")]
    private partial void LogUnchanged(DateTimeOffset builtAt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skill vocabulary refresh failed: {Code}: {Message}")]
    private partial void LogFailed(string code, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Skill vocabulary refresh crashed; retrying on the next tick")]
    private partial void LogCrashed(Exception exception);
}
