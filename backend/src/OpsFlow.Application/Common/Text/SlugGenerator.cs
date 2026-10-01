using System.Globalization;
using System.Text;

namespace OpsFlow.Application.Common.Text;

public static class SlugGenerator
{
    private const int MaxLength = 50;
    private const string Fallback = "company";
    private static readonly Dictionary<char, string> TurkishCharacterMap = new()
    {
        ['ı'] = "i", ['İ'] = "i", ['I'] = "i",
        ['ğ'] = "g", ['Ğ'] = "g",
        ['ü'] = "u", ['Ü'] = "u",
        ['ş'] = "s", ['Ş'] = "s",
        ['ö'] = "o", ['Ö'] = "o",
        ['ç'] = "c", ['Ç'] = "c"
    };

    public static string Generate(string input)
    {
        var mapped = new StringBuilder(input.Length);
        foreach (var character in input.Normalize(NormalizationForm.FormC))
        {
            if (TurkishCharacterMap.TryGetValue(character, out var replacement))
            {
                mapped.Append(replacement);
            }
            else
            {
                mapped.Append(character);
            }
        }

        var decomposed = mapped.ToString().Normalize(NormalizationForm.FormD);

        var slug = new StringBuilder(decomposed.Length);
        var lastWasDash = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);

            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(lower);
                lastWasDash = false;
            }
            else if (!lastWasDash && slug.Length > 0)
            {
                slug.Append('-');
                lastWasDash = true;
            }
        }

        var result = slug.ToString().Trim('-');

        if (result.Length > MaxLength)
        {
            result = result[..MaxLength].Trim('-');
        }

        return result.Length == 0 ? Fallback : result;
    }
}