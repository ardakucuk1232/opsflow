namespace OpsFlow.Application.Common.Interfaces;

public interface IRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}
