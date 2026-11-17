using Jobpilot.Application;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Jobpilot.Worker.UnitTests;

/// <summary>Runs the real handler with faked client and store; time is a FakeTimeProvider.</summary>
public sealed class SkillVocabularyRefresherTests : IAsyncDisposable
{
    private static readonly DateTimeOffset _builtAt = new(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2));

    private readonly ISkillVocabularyClient _client = Substitute.For<ISkillVocabularyClient>();
    private readonly ISkillVocabularyStore _store = Substitute.For<ISkillVocabularyStore>();
    private readonly FakeTimeProvider _time = new();
    private readonly FakeLogger<SkillVocabularyRefresher> _logger = new();
    private readonly ServiceProvider _services;
    private readonly SkillVocabularyRefresher _refresher;

    public SkillVocabularyRefresherTests()
    {
        _client.GetCompetenciesAsync(Arg.Any<CancellationToken>())
            .Returns(SkillVocabulary.Create(_builtAt, [new SkillConcept("Kafka", ["kafka"])]));

        _services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(_client)
            .AddSingleton(_store)
            .BuildServiceProvider();
        _refresher = new SkillVocabularyRefresher(_services.GetRequiredService<IServiceScopeFactory>(), _time, _logger);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _refresher.StopAsync(CancellationToken.None);
        _refresher.Dispose();
        await _services.DisposeAsync();
    }

    private int Downloads => _client.ReceivedCalls().Count();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition not met within 5 s.");
            }

            await Task.Delay(10, Ct);
        }
    }

    private Task WaitForLogAsync(int count) => WaitUntilAsync(() => _logger.Collector.Count >= count);

    [Fact]
    public async Task Refresh_RunsAtStart_ThenOncePerInterval()
    {
        await _refresher.StartAsync(Ct);
        await WaitForLogAsync(1);
        Downloads.ShouldBe(1);

        _time.Advance(SkillVocabularyRefresher.Interval - TimeSpan.FromMinutes(1));
        await Task.Delay(50, Ct);
        Downloads.ShouldBe(1);

        _time.Advance(TimeSpan.FromMinutes(1));
        await WaitForLogAsync(2);
        Downloads.ShouldBe(2);
    }

    [Fact]
    public async Task Refresh_Updated_LogsBuildAndCount()
    {
        await _refresher.StartAsync(Ct);
        await WaitForLogAsync(1);

        var log = _logger.LatestRecord;
        log.Level.ShouldBe(LogLevel.Information);
        log.Message.ShouldContain("updated");
        log.Message.ShouldContain("1 concepts");
    }

    [Fact]
    public async Task Refresh_AlreadyCurrent_LogsUnchanged()
    {
        _store.GetAsync(Arg.Any<CancellationToken>())
            .Returns(SkillVocabulary.Create(_builtAt, [new SkillConcept("Kafka", ["kafka"])]));

        await _refresher.StartAsync(Ct);
        await WaitForLogAsync(1);

        _logger.LatestRecord.Message.ShouldContain("unchanged");
    }

    [Fact]
    public async Task Refresh_Failure_IsLoggedAndRetriedNextTick()
    {
        _client.GetCompetenciesAsync(Arg.Any<CancellationToken>())
            .Returns(Error.Unavailable("Enrichments.Unavailable", "HTTP 503"));

        await _refresher.StartAsync(Ct);
        await WaitForLogAsync(1);
        _logger.LatestRecord.Level.ShouldBe(LogLevel.Warning);
        _logger.LatestRecord.Message.ShouldContain("Enrichments.Unavailable");

        _time.Advance(SkillVocabularyRefresher.Interval);
        await WaitForLogAsync(2);
        Downloads.ShouldBe(2);
    }

    [Fact]
    public async Task Refresh_Exception_IsLoggedAndRetriedNextTick()
    {
        var databaseDown = new InvalidOperationException("database down");
        _store.GetAsync(Arg.Any<CancellationToken>()).ThrowsAsync(databaseDown);

        await _refresher.StartAsync(Ct);
        await WaitForLogAsync(1);
        _logger.LatestRecord.Level.ShouldBe(LogLevel.Error);
        _logger.LatestRecord.Exception.ShouldBe(databaseDown);

        _time.Advance(SkillVocabularyRefresher.Interval);
        await WaitForLogAsync(2);
        Downloads.ShouldBe(2);
    }
}
