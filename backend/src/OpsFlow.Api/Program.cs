using System.Text.Json.Serialization;
using OpsFlow.Api.Extensions;
using OpsFlow.Api.Handlers;
using OpsFlow.Api.OpenApi;
using OpsFlow.Api.Middleware;
using OpsFlow.Api.Realtime;
using OpsFlow.Application;
using OpsFlow.Infrastructure;
using OpsFlow.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(ProblemDetailsDefaults.Configure);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();

builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddApiCors(builder.Configuration);

builder.Services.AddRealtime();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OpsFlowDbContext>(name: "postgres");

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>(NotificationsHub.Path);
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }