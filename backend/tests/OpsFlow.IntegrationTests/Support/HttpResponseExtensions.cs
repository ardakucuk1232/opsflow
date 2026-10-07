using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace OpsFlow.IntegrationTests.Support;

public static class HttpResponseExtensions
{
    public static async Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);

        return problem;
    }

    public static string? Code(this ProblemDetails problem) =>
        problem.Extensions.TryGetValue("code", out var code) ? code?.ToString() : null;
}
