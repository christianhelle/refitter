using AwesomeAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Refitter.Core.Validation;

namespace Refitter.Tests.OpenApi;

public class AttributeStringValidatorResponseTests
{
    [Test]
    public void Validate_Returns_When_Operation_Has_No_Responses()
    {
        OpenApiDiagnostic diagnostic = new() { SpecificationVersion = OpenApiSpecVersion.OpenApi3_0 };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/safe"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                }
                            },
                            Responses = null
                        }
                    }
                }
            }
        };

        var act = () => AttributeStringValidator.Validate(document, diagnostic);

        act.Should().NotThrow();
        diagnostic.Errors.Should().BeEmpty();
    }
}
