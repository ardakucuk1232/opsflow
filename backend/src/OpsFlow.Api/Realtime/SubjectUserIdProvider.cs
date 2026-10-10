using Microsoft.AspNetCore.SignalR;
using OpsFlow.Application.Common.Security;

namespace OpsFlow.Api.Realtime;

public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(OpsFlowClaimTypes.UserId)?.Value;
}
