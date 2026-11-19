using System.Text.RegularExpressions;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// Recognises senior roles from the title of an ad: senior, lead, principal, architect, head of and manager (chef).
/// Only the title counts. In the description the words are mostly about colleagues ("you will work with senior
/// developers"), so matching there would reject far too many ads. Measured on 491 IT ads, 26 % of the titles match.
/// </summary>
public static partial class SeniorTitles
{
    public static bool IsSenior(string headline)
    {
        ArgumentNullException.ThrowIfNull(headline);
        return SeniorWords().IsMatch(headline);
    }

    // "lead" only as a word of its own or in team/tech lead: it is also part of words like "leadership".
    [GeneratedRegex(
        @"senior|(?<![a-zåäö])lead(?![a-zåäö])|(?:team|tech)\s?lead|principal|arkitekt|architect|head of|chef",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex SeniorWords();
}
