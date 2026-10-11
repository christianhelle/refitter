using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class EnumDescriptionsSanitizerTests
{
    [Test]
    public void Converts_Object_To_Array_In_Enum_Order()
    {
        const string json = """
            {
              "type": "string",
              "enum": ["pending", "done"],
              "x-enumDescriptions": { "done": "Finished.", "pending": "Not started yet." }
            }
            """;

        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("Not started yet.", "Finished.");
        result["enum"]!.ToObject<string[]>().Should().Equal("pending", "done");
    }
}
