using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ClientNamingTests
{
    [TestMethod]
    public void Title_Uses_Sanitized_OpenApi_Title()
    {
        ClientNaming.Create(new NamingSettings(), "Swagger Petstore")
            .Title
            .Should()
            .Be("SwaggerPetstore");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Title_Falls_Back_To_InterfaceName_When_OpenApi_Title_Is_Missing(string? documentTitle)
    {
        ClientNaming.Create(new NamingSettings { InterfaceName = "MyClient" }, documentTitle)
            .Title
            .Should()
            .Be("MyClient");
    }

    [TestMethod]
    public void Title_Uses_InterfaceName_When_OpenApi_Title_Is_Disabled()
    {
        var naming = new NamingSettings { UseOpenApiTitle = false, InterfaceName = "MyClient" };

        ClientNaming.Create(naming, "Swagger Petstore")
            .Title
            .Should()
            .Be("MyClient");
    }

    [TestMethod]
    public void SerializerContextTypeName_Uses_Capitalized_OpenApi_Title()
    {
        ClientNaming.Create(new NamingSettings(), "petstore")
            .SerializerContextTypeName
            .Should()
            .Be("PetstoreSerializerContext");
    }

    [TestMethod]
    public void SerializerContextTypeName_Strips_Interface_Prefix_From_InterfaceName()
    {
        var naming = new NamingSettings { UseOpenApiTitle = false, InterfaceName = "IPetstore" };

        ClientNaming.Create(naming, null)
            .SerializerContextTypeName
            .Should()
            .Be("PetstoreSerializerContext");
    }

    [TestMethod]
    public void SerializerContextTypeName_Keeps_Names_That_Only_Start_With_I()
    {
        var naming = new NamingSettings { UseOpenApiTitle = false, InterfaceName = "Inventory" };

        ClientNaming.Create(naming, null)
            .SerializerContextTypeName
            .Should()
            .Be("InventorySerializerContext");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    public void SerializerContextTypeName_Falls_Back_To_Default_InterfaceName(string interfaceName)
    {
        var naming = new NamingSettings { UseOpenApiTitle = false, InterfaceName = interfaceName };

        ClientNaming.Create(naming, null)
            .SerializerContextTypeName
            .Should()
            .Be($"{NamingSettings.DefaultInterfaceName}SerializerContext");
    }

    [TestMethod]
    [DataRow("My Client", "MyClientSerializerContext")]
    [DataRow("My-Client", "MyClientSerializerContext")]
    public void SerializerContextTypeName_Is_A_Valid_Identifier(string interfaceName, string expected)
    {
        var naming = new NamingSettings { UseOpenApiTitle = false, InterfaceName = interfaceName };

        ClientNaming.Create(naming, null)
            .SerializerContextTypeName
            .Should()
            .Be(expected);
    }
}
