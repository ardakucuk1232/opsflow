using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Enums;

namespace OpsFlow.IntegrationTests.Support;

public static class TaskScenario
{
    public static async Task<TaskDetailDto> CreateTaskAsync(
        this TeamScenario team,
        Member actor,
        Guid projectId,
        string title,
        Guid? assigneeId = null)
    {
        var request = new CreateTaskRequest(title, null, TaskItemStatus.Todo, TaskPriority.Medium, assigneeId, null);
        var response = await team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/tasks", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskDetailDto>(JsonDefaults.Options);
        Assert.NotNull(task);

        return task;
    }
}
