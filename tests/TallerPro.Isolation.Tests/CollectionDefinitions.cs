using TallerPro.Isolation.Tests.Fixtures;
using Xunit;

namespace TallerPro.Isolation.Tests;

/// <summary>
/// xUnit collection definition for isolation test suite.
/// Shares a single SqlServerFixture across all isolation tests.
/// </summary>
[CollectionDefinition("SqlServerCollection")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class SqlServerCollectionDefinition : ICollectionFixture<SqlServerFixture>
{
}
