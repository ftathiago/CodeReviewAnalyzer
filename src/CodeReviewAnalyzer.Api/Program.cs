using CodeReviewAnalyzer.Api.Extensions;
using CodeReviewAnalyzer.Application.Services;
using CodeReviewAnalyzer.Database.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.ConfigureApplication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/pull-requests", async (
    [FromServices] PullRequestMetadataProcessor processor,
    [FromQuery] DateOnly begin,
    [FromQuery] DateOnly end) =>
{
    await processor.ExecuteAsync(begin, end);
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app
    .UseRouting()
    .UseEndpoints(endpoints => endpoints.MapControllers());

CodeReviewAnalyzerDatabaseExtensions.ExecuteMigration(app.Services);

await app.RunAsync();
