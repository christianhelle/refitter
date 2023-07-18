using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Refitter;

public static class SupportInformation
{
    public static string GetSupportKey()
        => GetAnonymousIdentity()[..7];

    public static string GetAnonymousIdentity()
        => $"{Environment.UserName}@{GetMachineName()}".ToSha256();

    [ExcludeFromCodeCoverage]
    private static string GetMachineName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch
        {
            return "localhost";
        }
    }

    private static string ToSha256(this string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).ToLowerInvariant();
    }
}