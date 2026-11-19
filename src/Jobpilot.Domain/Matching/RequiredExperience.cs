using System.Globalization;
using System.Text.RegularExpressions;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// Finds how many years of experience an ad asks for, from its description. Only clear statements count: a number
/// (digits or a word up to ten), "år"/"years", and then "erfarenhet"/"experience" within a few words, as in
/// "minst 3 års erfarenhet", "3–5 years of professional experience" or "tre års arbetslivserfarenhet".
/// Vague phrases ("ett par års erfarenhet") and the employer's own history ("vi har 20 års erfarenhet") do not count.
/// Measured on 491 IT ads, 122 state years (see docs/jobtech-api.md).
/// </summary>
public static partial class RequiredExperience
{
    /// <summary>Numbers above this are not a requirement on a person but noise, such as company history.</summary>
    private const int MaxPlausibleYears = 15;

    private static readonly Dictionary<string, int> _numberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ett"] = 1,
        ["en"] = 1,
        ["one"] = 1,
        ["två"] = 2,
        ["two"] = 2,
        ["tre"] = 3,
        ["three"] = 3,
        ["fyra"] = 4,
        ["four"] = 4,
        ["fem"] = 5,
        ["five"] = 5,
        ["sex"] = 6,
        ["six"] = 6,
        ["sju"] = 7,
        ["seven"] = 7,
        ["åtta"] = 8,
        ["eight"] = 8,
        ["nio"] = 9,
        ["nine"] = 9,
        ["tio"] = 10,
        ["ten"] = 10,
    };

    /// <summary>
    /// The lowest number of years the description clearly asks for, or null when it states none. For a range
    /// ("3–5 år") the lower bound counts. The lowest wins because a stricter-sounding sentence elsewhere is often
    /// about a nice-to-have.
    /// </summary>
    public static int? LowestRequiredYears(string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        int? lowest = null;
        foreach (Match match in YearsOfExperience().Matches(description))
        {
            var years = ToNumber(match.Groups["years"].Value);
            if (years <= MaxPlausibleYears && (lowest is null || years < lowest))
            {
                lowest = years;
            }
        }

        return lowest;
    }

    private static int ToNumber(string value) =>
        _numberWords.TryGetValue(value, out var number)
            ? number
            : int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);

    // The lookbehind skips the employer talking about itself: "vi har (över) 20 års erfarenhet".
    [GeneratedRegex(
        @"(?<!\b(?:vi|we)\s+(?:har|have)\s+(?:(?:över|over|mer än|more than)\s+)?)" +
        @"(?<![\p{L}\d])(?<years>\d{1,2}|ett|en|två|tre|fyra|fem|sex|sju|åtta|nio|tio|one|two|three|four|five|six|seven|eight|nine|ten)" +
        @"(?:\s*(?:-|–|—|till|to)\s*(?:\d{1,2}|\p{L}+))?\s*\+?\s*" +
        @"(?:års?|years?|yrs?)(?![\p{L}])['’]?" +
        @"(?:\s+[\p{L}-]+){0,3}?\s+[\p{L}-]*(?:erfarenhet|experience)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex YearsOfExperience();
}
