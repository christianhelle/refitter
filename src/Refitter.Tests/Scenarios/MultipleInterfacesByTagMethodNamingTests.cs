using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Test for Issue #672: Method naming increments globally instead of per-interface
/// https://github.com/christianhelle/refitter/issues/672
/// </summary>
public class MultipleInterfacesByTagMethodNamingTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Multi-Tag CRUD API
  version: '1.0.0'
paths:
  /users:
    get:
      tags: ['Users']
      operationId: 'GetAllUsers'
      responses:
        '200':
          description: 'Success'
    post:
      tags: ['Users']
      operationId: 'CreateUser'
      responses:
        '201':
          description: 'Created'
  /users/{id}:
    get:
      tags: ['Users']
      operationId: 'GetUserById'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Success'
    put:
      tags: ['Users']
      operationId: 'UpdateUser'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Success'
    delete:
      tags: ['Users']
      operationId: 'DeleteUser'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '204':
          description: 'No Content'
  /products:
    get:
      tags: ['Products']
      operationId: 'GetAllProducts'
      responses:
        '200':
          description: 'Success'
    post:
      tags: ['Products']
      operationId: 'CreateProduct'
      responses:
        '201':
          description: 'Created'
  /products/{id}:
    get:
      tags: ['Products']
      operationId: 'GetProductById'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Success'
    put:
      tags: ['Products']
      operationId: 'UpdateProduct'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Success'
    delete:
      tags: ['Products']
      operationId: 'DeleteProduct'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '204':
          description: 'No Content'
  /orders:
    get:
      tags: ['Orders']
      operationId: 'GetAllOrders'
      responses:
        '200':
          description: 'Success'
    post:
      tags: ['Orders']
      operationId: 'CreateOrder'
      responses:
        '201':
          description: 'Created'
  /orders/{id}:
    get:
      tags: ['Orders']
      operationId: 'GetOrderById'
      parameters:
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Success'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Separate_Interfaces_For_Each_Tag()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IUsersApi");
        generatedCode.Should().Contain("partial interface IProductsApi");
        generatedCode.Should().Contain("partial interface IOrdersApi");
    }

    [Test]
    public async Task Users_Interface_Should_Not_Have_Numbered_Method_Names()
    {
        string generatedCode = await GenerateCode();

        // Users interface should have method names WITHOUT numeric suffixes
        generatedCode.Should().Contain("Task<IApiResponse> GetAllUsers(");
        generatedCode.Should().Contain("Task<IApiResponse> CreateUser(");
        generatedCode.Should().Contain("Task<IApiResponse> GetUserById(");
        generatedCode.Should().Contain("Task<IApiResponse> UpdateUser(");
        generatedCode.Should().Contain("Task<IApiResponse> DeleteUser(");

        // Should NOT contain numbered versions in Users interface
        generatedCode.Should().NotContain("GetAllUsers2");
        generatedCode.Should().NotContain("CreateUser2");
        generatedCode.Should().NotContain("GetUserById2");
    }

    [Test]
    public async Task Products_Interface_Should_Not_Have_Numbered_Method_Names()
    {
        string generatedCode = await GenerateCode();

        // Products interface should have method names WITHOUT numeric suffixes
        generatedCode.Should().Contain("Task<IApiResponse> GetAllProducts(");
        generatedCode.Should().Contain("Task<IApiResponse> CreateProduct(");
        generatedCode.Should().Contain("Task<IApiResponse> GetProductById(");
        generatedCode.Should().Contain("Task<IApiResponse> UpdateProduct(");
        generatedCode.Should().Contain("Task<IApiResponse> DeleteProduct(");

        // Should NOT contain numbered versions
        generatedCode.Should().NotContain("GetAllProducts2");
        generatedCode.Should().NotContain("CreateProduct2");
    }

    [Test]
    public async Task Orders_Interface_Should_Not_Have_Numbered_Method_Names()
    {
        string generatedCode = await GenerateCode();

        // Orders interface should have method names WITHOUT numeric suffixes
        generatedCode.Should().Contain("Task<IApiResponse> GetAllOrders(");
        generatedCode.Should().Contain("Task<IApiResponse> CreateOrder(");
        generatedCode.Should().Contain("Task<IApiResponse> GetOrderById(");

        // Should NOT contain numbered versions
        generatedCode.Should().NotContain("GetAllOrders2");
        generatedCode.Should().NotContain("CreateOrder2");
    }

    [Test]
    public async Task Method_Names_Should_Be_Identical_Across_Different_Interfaces()
    {
        string generatedCode = await GenerateCode();

        // All interfaces have similar patterns (GetAll*, Create*, Get*ById)
        // These should NOT have global numbering

        // Extract just the method signatures to analyze
        var lines = generatedCode.Split('\n');

        // Look for method declarations with Task<IApiResponse>
        var methodDeclarations = lines
            .Where(l => l.Contains("Task<IApiResponse>") && l.Contains("("))
            .Select(l => l.Trim())
            .ToList();

        // Check that we have the expected method names without numeric suffixes
        methodDeclarations.Should().Contain(m => m.Contains("GetAllUsers("));
        methodDeclarations.Should().Contain(m => m.Contains("GetAllProducts("));
        methodDeclarations.Should().Contain(m => m.Contains("GetAllOrders("));

        // Verify NO methods have numeric suffixes like GetAll*2, Create*2, etc.
        methodDeclarations.Should().NotContain(m =>
            m.Contains("GetAll") && (m.Contains("2(") || m.Contains("3(")));
        methodDeclarations.Should().NotContain(m =>
            m.Contains("Create") && (m.Contains("2(") || m.Contains("3(")));
        methodDeclarations.Should().NotContain(m =>
            m.Contains("GetById") && (m.Contains("2(") || m.Contains("3(")));
    }

    [Test]
    public async Task Test_MultipleInterfacesByTag_DuplicateOperationIds_NoGlobalCounter()
    {
        // This is the core bug test: operationIds are identical across tags
        // but method names should NOT have global numbering
        string generatedCode = await GenerateCode();

        // Parse the code to find all method names
        var lines = generatedCode.Split('\n');
        var methodNames = lines
            .Where(l => l.Contains("Task<IApiResponse>") && l.Contains("("))
            .Select(l => l.Trim())
            .ToList();

        // CRITICAL: No method should have numeric suffixes from global counter
        // Each interface should have clean method names based on their operationIds
        methodNames.Should().Contain(m => m.Contains("GetAllUsers("));
        methodNames.Should().Contain(m => m.Contains("GetAllProducts("));
        methodNames.Should().Contain(m => m.Contains("GetAllOrders("));

        // The bug would manifest as GetAllUsers, GetAllProducts2, GetAllOrders3
        // We should NOT see this:
        methodNames.Should().NotContain(m => m.Contains("GetAllProducts2("),
            "Products interface should have GetAllProducts() not GetAllProducts2()");
        methodNames.Should().NotContain(m => m.Contains("GetAllOrders2("),
            "Orders interface should have GetAllOrders() not GetAllOrders2()");
        methodNames.Should().NotContain(m => m.Contains("GetAllOrders3("),
            "Orders interface should have GetAllOrders() not GetAllOrders3()");
    }

    [Test]
    public async Task Test_MultipleInterfacesByTag_NoConflict_WithinInterface()
    {
        // Test that a single operation in an interface doesn't get numbered
        string generatedCode = await GenerateCode();

        // GetOrderById appears only once in Orders interface
        // Should NOT be GetOrderById1
        generatedCode.Should().Contain("Task<IApiResponse> GetOrderById(");
        generatedCode.Should().NotContain("GetOrderById1(");
        generatedCode.Should().NotContain("GetOrderById2(");
    }

    [Test]
    public async Task Test_MultipleInterfacesByTag_EachInterface_HasOwnNamespace()
    {
        // Verify method naming is scoped per interface, not globally
        string generatedCode = await GenerateCode();

        // Extract interfaces
        var usersInterfaceStart = generatedCode.IndexOf("partial interface IUsersApi", StringComparison.Ordinal);
        var productsInterfaceStart = generatedCode.IndexOf("partial interface IProductsApi", StringComparison.Ordinal);
        var ordersInterfaceStart = generatedCode.IndexOf("partial interface IOrdersApi", StringComparison.Ordinal);

        usersInterfaceStart.Should().BeGreaterThan(0);
        productsInterfaceStart.Should().BeGreaterThan(0);
        ordersInterfaceStart.Should().BeGreaterThan(0);

        // Each interface should have its own clean set of methods
        var usersInterface = generatedCode.Substring(usersInterfaceStart, productsInterfaceStart - usersInterfaceStart);
        var productsInterface = generatedCode.Substring(productsInterfaceStart, ordersInterfaceStart - productsInterfaceStart);

        // Users interface should have GetAllUsers without any suffix
        usersInterface.Should().Contain("GetAllUsers(");
        usersInterface.Should().NotContain("GetAllUsers2");

        // Products interface should have GetAllProducts without any suffix
        productsInterface.Should().Contain("GetAllProducts(");
        productsInterface.Should().NotContain("GetAllProducts2");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            ReturnIApiResponse = true,
            ImmutableRecords = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
