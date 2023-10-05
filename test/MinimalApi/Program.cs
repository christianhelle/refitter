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
                return Results.Ok(await petstore.GetPetById(id));
            }
            catch (Refit.ApiException e)
            {
                return Results.StatusCode((int)e.StatusCode);
            }
        })
    .WithName("GetPetById")
    .WithOpenApi();

app.UseHttpsRedirection();
app.UseSwaggerUI();
app.UseSwagger();
app.Run();

