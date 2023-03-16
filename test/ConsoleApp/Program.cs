using Refit;
using System;
using System.Threading.Tasks;

namespace MyNamespace;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = RestService.For<Petstore.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
        var pet = await client.GetPetById(2);

        Console.WriteLine($"Name: {pet.Name}");
        Console.WriteLine($"Category: {pet.Category.Name}");
        Console.WriteLine($"Status: {pet.Status}");

        var pets = await client.FindPetsByStatus(Petstore.Status.Available);
        Console.WriteLine("Found " + pets.Count + " available pet(s)");        

        var taggedPets = await client.FindPetsByTags(new[] {"tag1Updated", "new"});
        Console.WriteLine("Found " + taggedPets.Count + " tagged pet(s)");
    }
}