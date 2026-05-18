namespace HoneyDrunk.Kernel.Abstractions.Identity;

internal static class UlidIdentity
{
    public static Ulid Parse(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (!Ulid.TryParse(value, out var ulid))
        {
            throw new ArgumentException("Value is not a valid ULID.", paramName);
        }

        return ulid;
    }

    public static bool TryParse(string? value, out Ulid ulid)
    {
        if (!string.IsNullOrWhiteSpace(value) && Ulid.TryParse(value, out ulid))
        {
            return true;
        }

        ulid = default;
        return false;
    }
}
