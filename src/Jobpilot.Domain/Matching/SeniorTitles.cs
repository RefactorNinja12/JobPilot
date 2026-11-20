using System.Text.RegularExpressions;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// Recognises senior roles from the title of an ad: senior, lead, principal, architect, head of and manager (chef).
/// Only the title counts. In the description the words are mostly about colleagues ("you will work with senior
/// developers"), so matching there would reject far too many ads. Measured on 491 IT ads, 26 % of the titles match.
/// </summary>
public static partial class SeniorTitles
{
    public static bool IsSenior(string headline) => IsSenior(headline, SeniorWords());

    /// <summary>A regex that runs out of time counts as "not senior": missing information never rejects an ad.</summary>
    internal static bool IsSenior(string headline, Regex seniorWords)
    {
        ArgumentNullException.ThrowIfNull(headline);
        try
        {
            return seniorWords.IsMatch(headline);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    // "lead" only as a word of its own or in team/tech lead: it is also part of words like "leadership".
    [GeneratedRegex(
        @"senior|(?<![a-zåäö])lead(?![a-zåäö])|(?:team|tech)\s?lead|principal|arkitekt|architect|head of|chef",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex SeniorWords();
}
