// See https://aka.ms/new-console-template for more information

using Refit;
using Refitter.MSBuild.Tests.Petstore;

var client = RestService.For<ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
var pet = await client.GetPetById(1);

Console.WriteLine($"Name: {pet.Name}");
Console.WriteLine($"Status: {pet.Status}");

var pets = await client.FindPetsByStatus(Status.Available);
Console.WriteLine("Found " + pets.Count + " available pet(s)");

var taggedPets = await client.FindPetsByTags(["tag1Updated", "new"]);
Console.WriteLine("Found " + taggedPets.Count + " tagged pet(s)");

Console.WriteLine();
