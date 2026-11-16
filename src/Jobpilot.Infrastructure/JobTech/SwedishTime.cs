namespace Jobpilot.Infrastructure.JobTech;

/// <summary>JobTech APIs send and expect Swedish local time without an offset.</summary>
internal static class SwedishTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
}
