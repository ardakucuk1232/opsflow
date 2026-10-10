using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Realtime;
using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Api.Extensions;

public static class RealtimeExtensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        return services;
    }
}
