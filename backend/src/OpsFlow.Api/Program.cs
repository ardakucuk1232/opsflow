using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging - Serilog

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Services

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var app =builder.Build();

// Pipeline

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();

    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/openapi/v1.json", "OpsFlow API v1");
        options.DocumentTitle = "OpsFlow API";
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Programm { }
