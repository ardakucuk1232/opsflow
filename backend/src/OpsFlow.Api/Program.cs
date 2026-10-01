using OpsFlow.Api.Handlers;
using OpsFlow.Application;
using OpsFlow.Infrastructure;
using OpsFlow.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OpsFlowDbContext>(name: "postgres");

var app = builder.Build();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpsFlow API v1");
        options.DocumentTitle = "OpsFlow API";
    });
}

app.UseHttpsRedirection();


app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }