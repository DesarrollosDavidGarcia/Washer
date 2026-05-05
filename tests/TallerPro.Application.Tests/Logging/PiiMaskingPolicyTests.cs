using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using TallerPro.Domain.Common;
using TallerPro.Infrastructure.Logging;
using Xunit;

namespace TallerPro.Application.Tests.Logging;

// ---------------------------------------------------------------------------
// InMemorySink — captures rendered log events for assertion
// ---------------------------------------------------------------------------
file sealed class InMemorySink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent) => _events.Add(logEvent);
}

// ---------------------------------------------------------------------------
// Fake domain types used in tests — no dependency on User/PlatformAdmin
// ---------------------------------------------------------------------------
file sealed class FakeUser
{
    public FakeUser(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
    }

    [PiiData(PiiLevel.High)]
    public string Email { get; }

    [PiiData(PiiLevel.Low)]
    public string DisplayName { get; }

    public int OrderCount { get; init; }
}

file sealed class FakePlatformAdmin
{
    public FakePlatformAdmin(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
    }

    [PiiData(PiiLevel.High)]
    public string Email { get; }

    [PiiData(PiiLevel.Low)]
    public string DisplayName { get; }
}

file sealed class PlainObject
{
    public string Name { get; init; } = string.Empty;
    public int Value { get; init; }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
file static class LogHelper
{
    public static string CaptureDestructured(object obj)
    {
        var sink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .Destructure.With(new PiiMaskingPolicy())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test {@Obj}", obj);

        sink.Events.Count.ShouldBeGreaterThan(0);
        var property = sink.Events[0].Properties["Obj"];
        return property.ToString();
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------
public class PiiMaskingPolicyTests
{
    // Test 1: High PII email → ***@*** and no original value leaks
    [Fact]
    public void TryDestructure_HighPiiProperty_MasksToStarAtStar()
    {
        var user = new FakeUser("foo@bar.com", "Foo Bar");

        var rendered = LogHelper.CaptureDestructured(user);

        rendered.ShouldContain("***@***");
        rendered.ShouldNotContain("foo@bar.com");
        rendered.ShouldNotContain("foo");
        rendered.ShouldNotContain("bar.com");
    }

    // Test 2: Low PII displayName → first char + ***
    [Fact]
    public void TryDestructure_LowPiiProperty_MasksToFirstCharPlusStar()
    {
        var user = new FakeUser("u@example.com", "Maria");

        var rendered = LogHelper.CaptureDestructured(user);

        rendered.ShouldContain("M***");
        rendered.ShouldNotContain("Maria");
    }

    // Test 3: Object without PII attributes → properties serialized as-is
    [Fact]
    public void TryDestructure_NoPiiAttributes_PassesThroughUnmasked()
    {
        var plain = new PlainObject { Name = "TallerPro", Value = 42 };

        var sink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .Destructure.With(new PiiMaskingPolicy())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test {@Obj}", plain);

        sink.Events.Count.ShouldBeGreaterThan(0);
        // Policy returns false for objects with no PII — Serilog uses default destructuring
        var rendered = sink.Events[0].Properties["Obj"].ToString();
        rendered.ShouldContain("TallerPro");
        rendered.ShouldContain("42");
    }

    // Test 4: Generic type check — FakePlatformAdmin also gets masked
    [Fact]
    public void TryDestructure_PlatformAdminFake_AppliesPolicyGenericly()
    {
        var admin = new FakePlatformAdmin("admin@platform.com", "Alice");

        var rendered = LogHelper.CaptureDestructured(admin);

        rendered.ShouldContain("***@***");
        rendered.ShouldNotContain("admin@platform.com");
        rendered.ShouldContain("A***");
        rendered.ShouldNotContain("Alice");
    }

    // Test 5: Edge — empty string on High PII → ***@*** (no exception)
    [Fact]
    public void TryDestructure_HighPii_EmptyString_ReturnsStarAtStar()
    {
        // FakeUser constructor would reject empty email, so test the policy directly
        var policy = new PiiMaskingPolicy();
        var config = new LoggerConfiguration()
            .Destructure.With(policy)
            .WriteTo.Sink(new InMemorySink());

        // Use a type with an explicitly empty value via a dedicated fake
        var obj = new FakeEmptyEmail();
        var sink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .Destructure.With(new PiiMaskingPolicy())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test {@Obj}", obj);

        var rendered = sink.Events[0].Properties["Obj"].ToString();
        rendered.ShouldContain("***@***");
    }

    // Test 6: Edge — single char on Low PII → char + ***
    [Fact]
    public void TryDestructure_LowPii_SingleCharValue_ReturnsCharPlusStar()
    {
        var obj = new FakeSingleCharName();
        var sink = new InMemorySink();
        var logger = new LoggerConfiguration()
            .Destructure.With(new PiiMaskingPolicy())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test {@Obj}", obj);

        var rendered = sink.Events[0].Properties["Obj"].ToString();
        rendered.ShouldContain("M***");
    }
}

// Edge-case fakes need to bypass the domain entity constructors
file sealed class FakeEmptyEmail
{
    [PiiData(PiiLevel.High)]
    public string Email { get; } = string.Empty;
}

file sealed class FakeSingleCharName
{
    [PiiData(PiiLevel.Low)]
    public string DisplayName { get; } = "M";
}
