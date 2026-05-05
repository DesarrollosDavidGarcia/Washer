using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests;

/// <summary>
/// xUnit collection definition for SQL Server fixture sharing across integration tests.
/// Any test class decorated with [Collection("SqlServerCollection")] will share the same fixture instance.
/// </summary>
[CollectionDefinition("SqlServerCollection")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class SqlServerCollectionDefinition : ICollectionFixture<SqlServerFixture>
{
}
