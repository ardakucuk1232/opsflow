using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using OpsFlow.Application.Features.Attachments;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AttachmentEndpointsTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x01, 0x02];

    private readonly OpsFlowApiFactory _factory;
    private readonly HttpClient _client;
    private readonly TeamScenario _team;

    public AttachmentEndpointsTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
        _team = new TeamScenario(factory, _client);
    }

    [Fact]
    public async Task Upload_StoresTheFile_AndItCanBeListedAndDownloaded()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await _team.CreateTaskAsync(admin, project.Id, "With files");

        var response = await UploadAsync(admin, task.Id, "../../notes.txt", Encoding.UTF8.GetBytes("Toplantı notları"), "application/x-msdownload");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>(JsonDefaults.Options);
        Assert.NotNull(attachment);
        Assert.Equal("notes.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal(admin.Id, attachment.UploadedBy.Id);
        Assert.True(attachment.CanDelete);

        var list = await _team.GetAsync<List<AttachmentDto>>(admin, $"/api/tasks/{task.Id}/attachments");
        Assert.Equal(attachment.Id, Assert.Single(list).Id);

        var download = await _team.SendAsync(admin, HttpMethod.Get, $"/api/attachments/{attachment.Id}/content");

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/plain", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("Toplantı notları", Encoding.UTF8.GetString(await download.Content.ReadAsByteArrayAsync()));
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("no-extension")]
    public async Task Upload_RejectsFileTypesOutsideTheAllowList(string fileName)
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Files");

        var response = await UploadAsync(admin, task.Id, fileName, Png);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Attachments.FileTypeNotAllowed, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Upload_RejectsAFileWhoseContentDoesNotMatchItsExtension()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Files");

        var response = await UploadAsync(admin, task.Id, "photo.png", Encoding.UTF8.GetBytes("<script>alert(1)</script>"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Attachments.ContentMismatch, (await response.ReadProblemAsync()).Code());
        Assert.Empty(await _team.GetAsync<List<AttachmentDto>>(admin, $"/api/tasks/{task.Id}/attachments"));
    }

    [Fact]
    public async Task Upload_RejectsFilesOverTheSizeLimit_AndEmptyFiles()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Files");

        var tooLarge = await UploadAsync(admin, task.Id, "big.txt", new byte[OpsFlowApiFactory.MaxFileSizeBytes + 1]);
        var empty = await UploadAsync(admin, task.Id, "empty.txt", []);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLarge.StatusCode);
        Assert.Equal(ErrorCodes.Attachments.FileTooLarge, (await tooLarge.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task AttachmentsOfAProjectTheUserIsNotIn_AreInvisible()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Secret");
        var attachment = await UploadOkAsync(admin, task.Id, "secret.png", Png);

        var list = await _team.SendAsync(employee, HttpMethod.Get, $"/api/tasks/{task.Id}/attachments");
        var upload = await UploadAsync(employee, task.Id, "mine.png", Png);
        var download = await _team.SendAsync(employee, HttpMethod.Get, $"/api/attachments/{attachment.Id}/content");
        var delete = await _team.SendAsync(employee, HttpMethod.Delete, $"/api/attachments/{attachment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task AttachmentsOfAnotherCompany_AreInvisibleAndUntouchable()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(adminA);
        var task = await _team.CreateTaskAsync(adminA, project.Id, "Company A task");
        var attachment = await UploadOkAsync(adminA, task.Id, "contract.png", Png);

        var list = await _team.SendAsync(adminB, HttpMethod.Get, $"/api/tasks/{task.Id}/attachments");
        var upload = await UploadAsync(adminB, task.Id, "injected.png", Png);
        var download = await _team.SendAsync(adminB, HttpMethod.Get, $"/api/attachments/{attachment.Id}/content");
        var delete = await _team.SendAsync(adminB, HttpMethod.Delete, $"/api/attachments/{attachment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Single(await _team.GetAsync<List<AttachmentDto>>(adminA, $"/api/tasks/{task.Id}/attachments"));
    }

    [Fact]
    public async Task Delete_IsAllowedForTheUploaderOrSomeoneWhoCanDeleteTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var uploader = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var colleague = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, uploader.Id);
        await _team.AddProjectMemberAsync(admin, project.Id, colleague.Id);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Shared");

        var first = await UploadOkAsync(uploader, task.Id, "first.png", Png);
        var second = await UploadOkAsync(uploader, task.Id, "second.png", Png);

        var asSeenByColleague = await _team.GetAsync<List<AttachmentDto>>(colleague, $"/api/tasks/{task.Id}/attachments");
        Assert.All(asSeenByColleague, a => Assert.False(a.CanDelete));

        var byColleague = await _team.SendAsync(colleague, HttpMethod.Delete, $"/api/attachments/{first.Id}");
        var byUploader = await _team.SendAsync(uploader, HttpMethod.Delete, $"/api/attachments/{first.Id}");
        var byAdmin = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/attachments/{second.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, byColleague.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byUploader.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Empty(await _team.GetAsync<List<AttachmentDto>>(admin, $"/api/tasks/{task.Id}/attachments"));

        var download = await _team.SendAsync(admin, HttpMethod.Get, $"/api/attachments/{first.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(_factory.StorageRoot, $"{first.Id:N}", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Upload_RequiresTheUploadPermission()
    {
        var admin = await _team.RegisterAdminAsync();
        await _team.CreateRoleAsync(admin, "Okuyucu", PermissionCodes.UserView);
        var reader = await _team.AddMemberAsync(admin, "Okuyucu");
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, reader.Id);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Read only");

        var upload = await UploadAsync(reader, task.Id, "note.txt", Encoding.UTF8.GetBytes("hi"));
        var list = await _team.SendAsync(reader, HttpMethod.Get, $"/api/tasks/{task.Id}/attachments");

        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    private async Task<AttachmentDto> UploadOkAsync(Member actor, Guid taskId, string fileName, byte[] content)
    {
        var response = await UploadAsync(actor, taskId, fileName, content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>(JsonDefaults.Options);
        Assert.NotNull(attachment);

        return attachment;
    }

    private async Task<HttpResponseMessage> UploadAsync(
        Member actor,
        Guid taskId,
        string fileName,
        byte[] content,
        string contentType = "application/octet-stream")
    {
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var form = new MultipartFormDataContent { { file, "file", fileName } };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/attachments") { Content = form };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor.AccessToken);

        return await _client.SendAsync(request);
    }
}
