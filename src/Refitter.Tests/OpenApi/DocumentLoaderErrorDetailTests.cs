using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// The error of each strategy reaches the user when a document cannot be loaded (#1424).
/// </summary>
[TestClass]
public class DocumentLoaderErrorDetailTests
{
    // A collectionFormat that is not a string cannot be read, and is not rewritten
    private const string UnreadableSpec = """
        {
          "swagger": "2.0",
          "info": { "title": "T", "version": "1.0" },
          "paths": {
            "/items": {
              "get": {
                "parameters": [
                  { "name": "ids", "in": "query", "type": "array", "items": { "type": "string" }, "collectionFormat": { "x": 1 } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    [TestMethod]
    public async Task Failure_Message_Includes_The_Parser_Error()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(UnreadableSpec);

        var act = async () => await new DocumentLoader().LoadAsync(swaggerFile);

        var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        exception.Message.Should().Contain("FileDocumentStrategy:");
        exception.Message.Should().Contain("collectionFormat");
    }
}
