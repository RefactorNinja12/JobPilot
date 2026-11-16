using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;

namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary>Translates a JobSearch ad into the domain <see cref="JobAd"/>.</summary>
public static class JobSearchAdMapper
{
    // JobTech Taxonomy concept ids for working_hours_type.
    private const string FullTimeConceptId = "6YE1_gAC_R2G";
    private const string PartTimeConceptId = "947z_JGS_Uk2";

    private static readonly TimeZoneInfo _swedishTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    public static Result<JobAd> ToJobAd(JobSearchAdDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var ad = dto.Id ?? "?";

        if (dto.PublicationDate is not { } published)
        {
            return JobSearchErrors.MissingField(ad, "publication_date");
        }

        if (dto.ApplicationDeadline is not { } deadline)
        {
            return JobSearchErrors.MissingField(ad, "application_deadline");
        }

        // Only parses here; JobAd.Create enforces http(s).
        if (!Uri.TryCreate(dto.WebpageUrl, UriKind.Absolute, out var url))
        {
            return JobSearchErrors.InvalidUrl(ad);
        }

        var employer = Employer.Create(dto.Employer?.Name, dto.Employer?.OrganizationNumber);
        if (employer.IsFailure)
        {
            return employer.Error;
        }

        var occupation = Occupation.Create(dto.Occupation?.ConceptId, dto.Occupation?.Label);
        if (occupation.IsFailure)
        {
            return occupation.Error;
        }

        var details = new JobAdDetails(
            url,
            employer.Value,
            ToWorkplace(dto.WorkplaceAddress),
            occupation.Value,
            ToWorkingHours(dto.WorkingHoursType),
            ToSwedishTime(published),
            DateOnly.FromDateTime(deadline));

        return JobAd.Create(dto.Id, dto.Headline, dto.Description?.Text, details);
    }

    /// <summary>
    /// JobSearch sends coordinates as [longitude, latitude], the reverse of <see cref="Coordinates.Create"/>.
    /// Missing or invalid coordinates give a workplace without coordinates, never a failed ad.
    /// </summary>
    private static Workplace ToWorkplace(JobSearchAddressDto? address)
    {
        if (address is null)
        {
            return Workplace.Unknown;
        }

        Coordinates? coordinates = null;
        if (address.Coordinates is [{ } longitude, { } latitude])
        {
            var created = Coordinates.Create(latitude, longitude);
            coordinates = created.IsSuccess ? created.Value : null;
        }

        return Workplace.Create(address.Municipality, coordinates);
    }

    private static WorkingHours ToWorkingHours(JobSearchConceptDto? concept) => concept?.ConceptId switch
    {
        FullTimeConceptId => WorkingHours.FullTime,
        PartTimeConceptId => WorkingHours.PartTime,
        _ => WorkingHours.Unknown,
    };

    private static DateTimeOffset ToSwedishTime(DateTime local) =>
        new(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _swedishTime.GetUtcOffset(local));
}
