namespace OpsFlow.Application.Common.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; init; } = "App_Data/uploads";

    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
}
