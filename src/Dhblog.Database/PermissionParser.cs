namespace Dhblog.Database;

[Flags]
public enum PermissionFlags
{
    None = 0,
    Read = 1,
    Write = 2
}

public static class PermissionParser
{
    /// <summary>
    /// When a FeatureRole exists, Read is assumed true and Write false unless overridden.
    /// </summary>
    public static PermissionFlags Parse(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
            return PermissionFlags.Read;

        var result = PermissionFlags.Read;
        var writeGranted = false;
        var readRevoked = false;

        foreach (var token in permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var t = token.Trim();
            if (t.Length == 0) continue;

            var revoke = t.StartsWith('-');
            var grant = t.StartsWith('+');
            var code = (revoke || grant ? t[1..] : t).ToUpperInvariant();

            switch (code)
            {
                case "R":
                    if (revoke) readRevoked = true;
                    break;
                case "W":
                    if (revoke) writeGranted = false;
                    else writeGranted = true;
                    break;
            }
        }

        if (readRevoked)
            result &= ~PermissionFlags.Read;
        if (writeGranted)
            result |= PermissionFlags.Write;

        return result;
    }

    public static bool HasRead(PermissionFlags flags) => flags.HasFlag(PermissionFlags.Read);
    public static bool HasWrite(PermissionFlags flags) => flags.HasFlag(PermissionFlags.Write);
}
