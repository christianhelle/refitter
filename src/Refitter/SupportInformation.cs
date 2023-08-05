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
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).ToLowerInvariant();
    }
}