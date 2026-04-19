namespace UUcars.Tests.Integration;

// 定义 "Integration" 这个 Collection
// 所有标注了 [Collection("Integration")] 的测试类共享同一个 SqlServerTestFactory 实例
// 这意味着：整个集成测试只启动一次 SQL Server 容器，显著减少测试时间
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<SqlServerTestFactory>
{
    // 这个类本身不包含任何代码
    // 它只是一个标记，告诉 xUnit：
    // "Integration" 这个 Collection 使用 SqlServerTestFactory 作为共享 Fixture
}