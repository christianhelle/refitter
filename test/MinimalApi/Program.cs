using Petstore;

using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging(logging => logging.AddDebug());
builder.Services.AddTransient<TelemetryDelegatingHandler>();
builder.Services.ConfigureRefitClients();

var app = builder.Build();
app.MapGet(
        "/pet/{id:long}",
        async (ISwaggerPetstore petstore, long id) =>
        {
            try
            {
                var response = await petstore.GetPetById(id);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return Results.StatusCode((int)response.StatusCode);

                return Results.Ok(response.Content);
            }
            catch (Refit.ApiException e)
            {
                return Results.StatusCode((int)e.StatusCode);
            }
        })
    .WithName("GetPetById")
    .WithOpenApi();

app.MapDelete(
        "/pet/{id:long}",
        async (ISwaggerPetstore petstore, long id) =>
        {
            var response = await petstore.DeletePet(id, null);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return Results.Ok();
            else
                return Results.StatusCode((int)response.StatusCode);
        })
    .WithName("DeletePet")
    .WithOpenApi();

app.UseHttpsRedirection();
app.UseSwaggerUI();
app.UseSwagger();
app.Run();

