using Application;

using Infrastructure;

using WebApi;
using WebApi.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogger();

builder.Services
       .AddApplication()
       .AddPresentation(builder.Configuration)
       .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApiConfig();
    app.UseSwaggerUi();
    app.ApplyMigrations();
}

app.UseLogger();

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapAppControllers();

await app.RunAsync();