using Petstore;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging(logging => logging.AddDebug());
builder.Services.AddTransient<TelemetryDelegatingHandler>();
builder.Services.ConfigureRefitClients(
    settings: RefitSettings.ForJsonContext(SwaggerPetstoreSerializerContext.Default));

var app = builder.Build();
app.MapGet(
        "/pet/{id:long}",
        async (ISwaggerPetstore petstore, long id) =>
        {
            try
            {
                var response = await petstore.GetPetById(id);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return Results.StatusCode((int?)response.StatusCode ?? StatusCodes.Status502BadGateway);

                return Results.Ok(response.Content);
            }
            catch (Refit.ApiException e)
            {
                return Results.StatusCode((int)e.StatusCode);
            }
        })
    .WithName("GetPetById");

app.MapDelete(
        "/pet/{id:long}",
        async (ISwaggerPetstore petstore, long id) =>
        {
            var response = await petstore.DeletePet(id);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return Results.Ok();
            else
                return Results.StatusCode((int?)response.StatusCode ?? StatusCodes.Status502BadGateway);
        })
    .WithName("DeletePet");

app.UseHttpsRedirection();
app.UseSwaggerUI();
app.UseSwagger();
app.Run();

