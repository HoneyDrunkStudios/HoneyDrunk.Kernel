using System.Text.RegularExpressions;

namespace HoneyDrunk.Kernel.Abstractions.Identity;

internal static partial class KebabCaseIdentity
{
    private static readonly Regex ValidationPattern = ValidationRegex();

    public static bool IsValid(string? value, string displayName, int minLength, int maxLength, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = $"{displayName} cannot be null or whitespace.";
            return false;
        }

        if (value.Length < minLength || value.Length > maxLength)
        {
            errorMessage = $"{displayName} must be between {minLength} and {maxLength} characters.";
            return false;
        }

        if (!ValidationPattern.IsMatch(value))
        {
            errorMessage = $"{displayName} must be kebab-case: lowercase letters, digits, and hyphens only. Cannot have consecutive hyphens or start/end with hyphens.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();
}
