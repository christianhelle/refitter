global using System;

using Refit;
using System.Threading.Tasks;

namespace MyNamespace;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // await TestPetstoreUsingDirectTypes();
        // await TestPetstoreUsingApiResponse();
        // await TestPetstoreObservableTypes();
        await Task.Delay(0);
    }

    // private static async Task TestPetstoreUsingApiResponse()
    // {
    //     var responseClient = RestService.For<Petstore.UsingApiResponse.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
    //     var response = await responseClient.GetPetById(1);

    //     Console.WriteLine("## Using IApiResponse<T> as return type ##");
    //     Console.WriteLine($"HTTP Status Code: {response.StatusCode}");
    //     Console.WriteLine($"Name: {response.Content.Name}");
    //     Console.WriteLine($"Status: {response.Content.Status}");

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
    //     Console.WriteLine($"Status: {pet.Status}");

    //     var pets = await client.FindPetsByStatus(Petstore.Status.Available);
    //     Console.WriteLine("Found " + pets.Count + " available pet(s)");

    //     var taggedPets = await client.FindPetsByTags(new[] { "tag1Updated", "new" });
    //     Console.WriteLine("Found " + taggedPets.Count + " tagged pet(s)");

    //     Console.WriteLine();
    // }

    // private static async Task TestPetstoreObservableTypes()
    // {
    //     var tcs = new TaskCompletionSource<bool>();

    //     RestService
    //         .For<Petstore.UsingIObservable.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3")
    //         .GetPetById(1)
    //         .Subscribe(pet =>
    //         {
    //             Console.WriteLine($"Name: {pet.Name}");
    //             Console.WriteLine($"Status: {pet.Status}");
    //             tcs.SetResult(true);
    //         });

    //     await tcs.Task;
    // }
}