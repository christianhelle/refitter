using Refit;
using Test;

var api = RestService.For<ILMStudiov1RESTAPI>("http://localhost:1234");
var modelsResponse = await api.ListModels();

foreach (var model in modelsResponse.Models)
{
    Console.WriteLine($"Model ID: {model.Key}, Name: {model.DisplayName}");
}

var streamResponse = api.Chat(
    new ChatRequest
    {
        Model = modelsResponse.Models.First().Key,
        Input = "Hello! What can you do?",
        Stream = true,
        Reasoning = ChatRequestReasoning.On,
    }
);

Console.WriteLine("\n\nStreaming response:\n");

await foreach (var chunk in streamResponse)
{
    var type = chunk.AdditionalProperties.TryGetValue("type", out var typeValue)
        ? typeValue.ToString() : "unknown";
    var content = chunk.AdditionalProperties.TryGetValue("content", out var contentValue)
        ? contentValue.ToString()
        : "unknown";

    if (type == "reasoning.delta")
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(content);
    }
    else if (type == "reasoning.end")
    {
        Console.WriteLine(Environment.NewLine);
    }
    else if (type == "message.delta")
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(content);
    }
}

Console.WriteLine("\n\nStreaming finished.\n");
