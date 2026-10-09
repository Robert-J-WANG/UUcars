namespace UUcars.Tests.Integration;

// 同一个 Integration Collection 共享一套 API 集成测试环境。
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ApiIntegrationTestFactory>
{
}
