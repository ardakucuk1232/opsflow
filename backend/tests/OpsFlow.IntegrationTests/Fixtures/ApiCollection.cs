namespace OpsFlow.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<OpsFlowApiFactory>
{
    public const string Name = "Api";
}