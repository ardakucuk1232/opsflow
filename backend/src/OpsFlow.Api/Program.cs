using OpsFlow.Api.Extensions;
using OpsFlow.Api.Handlers;
using OpsFlow.Api.OpenApi;
using OpsFlow.Api.Middleware;
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
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OpsFlowDbContext>(name: "postgres");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpsFlow API v1");
        options.DocumentTitle = "OpsFlow API";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }