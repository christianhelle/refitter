using Refit;
using Refitter.MSBuild.Tests.Petstore;

#pragma warning disable S1075 // Hardcoded URI — this test intentionally targets the public Petstore API
const string PetstoreBaseUrl = "https://petstore3.swagger.io/api/v3";
#pragma warning restore S1075

try
{
    var client = RestService.For<ISwaggerPetstore>(PetstoreBaseUrl);
    var pet = await client.GetPetById(1);

    Console.WriteLine($"Name: {pet.Name}");
    Console.WriteLine($"Status: {pet.Status}");

    var pets = await client.FindPetsByStatus(Status.Available);
    Console.WriteLine("Found " + pets.Count + " available pet(s)");

    var taggedPets = await client.FindPetsByTags(["tag1Updated", "new"]);
    Console.WriteLine("Found " + taggedPets.Count + " tagged pet(s)");
}
catch (ApiException ex)
{
    Console.WriteLine($"API call failed ({ex.StatusCode}): {ex.Message}");
    Console.WriteLine($"    This is an external API issue, not a code generation problem.");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}

Console.WriteLine();
