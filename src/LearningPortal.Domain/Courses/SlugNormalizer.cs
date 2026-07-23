using System.Text;

namespace LearningPortal.Domain.Courses;

/// <summary>Normalizes course slugs to lowercase kebab-case.</summary>
public static class SlugNormalizer
{
    /// <summary>Normalizes a candidate slug.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingHyphen = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsWhiteSpace(character) || character is '_' or '-')
            {
                pendingHyphen = builder.Length > 0;
                continue;
            }

            if (!char.IsLetterOrDigit(character))
            {
                continue;
            }

            if (pendingHyphen)
            {
                builder.Append('-');
                pendingHyphen = false;
            }

            builder.Append(character);
        }

        return builder.ToString().Trim('-');
    }
}
