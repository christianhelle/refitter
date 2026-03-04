using FluentAssertions;
using TUnit.Core;

namespace Refitter.Tests;

public class SupportInformationTests
{
    [Test]
    public void GetSupportKey_Should_Return_7_Character_String()
    {
        var supportKey = SupportInformation.GetSupportKey();

        supportKey.Should().NotBeNullOrEmpty();
        supportKey.Length.Should().Be(7);
    }

    [Test]
    public void GetSupportKey_Should_Be_Consistent()
    {
        var key1 = SupportInformation.GetSupportKey();
        var key2 = SupportInformation.GetSupportKey();

        key1.Should().Be(key2);
    }

    [Test]
    public void GetAnonymousIdentity_Should_Return_Hash()
    {
        var identity = SupportInformation.GetAnonymousIdentity();

        identity.Should().NotBeNullOrEmpty();
        identity.Length.Should().BeGreaterThan(7);
    }

    [Test]
    public void GetAnonymousIdentity_Should_Be_Consistent()
    {
        var identity1 = SupportInformation.GetAnonymousIdentity();
        var identity2 = SupportInformation.GetAnonymousIdentity();

        identity1.Should().Be(identity2);
    }

    [Test]
    public void GetAnonymousIdentity_Should_Be_Lowercase()
    {
        var identity = SupportInformation.GetAnonymousIdentity();

        identity.Should().Be(identity.ToLowerInvariant());
    }

    [Test]
    public void GetSupportKey_Should_Be_First_7_Chars_Of_AnonymousIdentity()
    {
        var identity = SupportInformation.GetAnonymousIdentity();
        var supportKey = SupportInformation.GetSupportKey();

        supportKey.Should().Be(identity[..7]);
    }

    [Test]
    public void GetAnonymousIdentity_Should_Be_Base64_Encoded()
    {
        var identity = SupportInformation.GetAnonymousIdentity();

        // Base64 strings contain only alphanumeric characters, +, /, and =
        // Since it's lowercase, + becomes +, / becomes /, = becomes =
        identity.Should().MatchRegex("^[a-z0-9+/=]+$");
    }

    [Test]
    public void GetSupportKey_Should_Not_Be_Empty()
    {
        var supportKey = SupportInformation.GetSupportKey();

        supportKey.Should().NotBeEmpty();
    }

    [Test]
    public void GetAnonymousIdentity_Should_Not_Contain_Username_Directly()
    {
        var identity = SupportInformation.GetAnonymousIdentity();
        var username = Environment.UserName;

        // Hash should not contain the raw username
        identity.Should().NotContain(username);
    }
}
