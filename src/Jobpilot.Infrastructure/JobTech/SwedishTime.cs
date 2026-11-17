namespace Jobpilot.Infrastructure.JobTech;

/// <summary>JobTech APIs send and expect Swedish local time without an offset.</summary>
internal static class SwedishTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    /// <summary>Attaches the Swedish offset (CET or CEST) to a time JobTech sent without one.</summary>
    public static DateTimeOffset FromLocal(DateTime local) =>
        new(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone.GetUtcOffset(local));
}
