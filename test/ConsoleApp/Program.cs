using Refit;
using System;
using System.Threading.Tasks;

namespace MyNamespace;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // await TestPetstoreUsingDirectTypes();
        // await TestPetstoreUsingApiResponse();
    }

    // private static async Task TestPetstoreUsingApiResponse()
    // {
    //     var responseClient = RestService.For<Petstore.Interface.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
    //     IApiResponse<Petstore.Pet> pet = await responseClient.GetPetById(1);

    //     Console.WriteLine($"Name: {pet.Content.Name}");
    //     Console.WriteLine($"Category: {pet.Content.Category.Name}");
    //     Console.WriteLine($"Status: {pet.Content.Status}");

    //     var pets = await responseClient.FindPetsByStatus(Petstore.Status.Available);
    //     Console.WriteLine("Found " + pets.Content.Count + " available pet(s)");

    //     var taggedPets = await responseClient.FindPetsByTags(new[] { "tag1Updated", "new" });
    //     Console.WriteLine("Found " + taggedPets.Content.Count + " tagged pet(s)");
    // }

    // private static async Task TestPetstoreUsingDirectTypes()
    // {
    //     var client = RestService.For<Petstore.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
    //     var pet = await client.GetPetById(1);

    //     Console.WriteLine($"Name: {pet.Name}");
    //     Console.WriteLine($"Category: {pet.Category.Name}");
    //     Console.WriteLine($"Status: {pet.Status}");

    //     var pets = await client.FindPetsByStatus(Petstore.Status.Available);
    //     Console.WriteLine("Found " + pets.Count + " available pet(s)");

    //     var taggedPets = await client.FindPetsByTags(new[] { "tag1Updated", "new" });
    //     Console.WriteLine("Found " + taggedPets.Count + " tagged pet(s)");
    // }
}