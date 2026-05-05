using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using TallerPro.Analyzers;
using Xunit;

namespace TallerPro.Application.Tests.Analyzers;

/// <summary>
/// Tests for TP0001 — IgnoreQueryFilters() requires explicit authorization.
/// Each test embeds fake stubs so the analyzer runs without EF Core dependency.
/// </summary>
public class IgnoreQueryFiltersAnalyzerTests
{
    // Stubs placed before the SomeApp namespace so using directives inside it resolve correctly.
    // Defines: FakeSet<T> with IgnoreQueryFilters() extension, and AllowIgnoreQueryFiltersAttribute.
    // Lines 1-14 of the embedded source are these stubs (line numbers affect WithSpan calls below).
    private const string SharedStubs = """
        namespace EfFake
        {
            public class FakeSet<T> { }

            public static class FakeExtensions
            {
                public static EfFake.FakeSet<T> IgnoreQueryFilters<T>(this EfFake.FakeSet<T> set) => set;
            }
        }

        namespace TallerPro.Domain.Common
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class AllowIgnoreQueryFiltersAttribute : System.Attribute
            {
                public string Reason { get; }
                public AllowIgnoreQueryFiltersAttribute(string reason) { Reason = reason ?? string.Empty; }
            }
        }

        """;

    // ------------------------------------------------------------------ //
    // Test 1: call without attribute → TP0001 emitted as Error            //
    // Source structure after SharedStubs (lines 1-20):
    // 21: namespace SomeApp
    // 22: {
    // 23:     using EfFake;
    // 24:
    // 25:     public class SomeService
    // 26:     {
    // 27:         public void GetAll()
    // 28:         {
    // 29:             var db = new FakeSet<string>();
    // 30:             var result = db.IgnoreQueryFilters();
    // 31:         }
    // ------------------------------------------------------------------ //
    [Fact]
    public async Task IgnoreQueryFilters_WithoutAttribute_EmitsTP0001AsError()
    {
        var source = SharedStubs + """
            namespace SomeApp
            {
                using EfFake;

                public class SomeService
                {
                    public void GetAll()
                    {
                        var db = new FakeSet<string>();
                        var result = db.IgnoreQueryFilters();
                    }
                }
            }
            """;

        var test = new CSharpAnalyzerTest<IgnoreQueryFiltersAnalyzer, DefaultVerifier>
        {
            TestCode = source
        };

        // Severity must be Error (not Warning) — explicit check with exact span
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TP0001", DiagnosticSeverity.Error)
                .WithSpan(29, 26, 29, 49)
                .WithArguments("GetAll"));

        await test.RunAsync();
    }

    // ------------------------------------------------------------------ //
    // Test 2: method with valid [AllowIgnoreQueryFilters("reason")] → no diagnostic
    // ------------------------------------------------------------------ //
    [Fact]
    public async Task IgnoreQueryFilters_WithValidAttribute_EmitsNoDiagnostic()
    {
        var source = SharedStubs + """
            namespace SomeApp
            {
                using EfFake;
                using TallerPro.Domain.Common;

                public class SomeService
                {
                    [AllowIgnoreQueryFilters("Needed for soft-delete admin view")]
                    public void GetAllIncludingDeleted()
                    {
                        var db = new FakeSet<string>();
                        var result = db.IgnoreQueryFilters();
                    }
                }
            }
            """;

        var test = new CSharpAnalyzerTest<IgnoreQueryFiltersAnalyzer, DefaultVerifier>
        {
            TestCode = source
        };

        // no diagnostics expected
        await test.RunAsync();
    }

    // ------------------------------------------------------------------ //
    // Test 3: attribute with empty string Reason → TP0001 emitted         //
    // Line 31 because of extra using + attribute lines vs Test 1           //
    // ------------------------------------------------------------------ //
    [Fact]
    public async Task IgnoreQueryFilters_WithAttributeEmptyReason_EmitsTP0001()
    {
        var source = SharedStubs + """
            namespace SomeApp
            {
                using EfFake;
                using TallerPro.Domain.Common;

                public class SomeService
                {
                    [AllowIgnoreQueryFilters("")]
                    public void GetAllBad()
                    {
                        var db = new FakeSet<string>();
                        var result = db.IgnoreQueryFilters();
                    }
                }
            }
            """;

        var test = new CSharpAnalyzerTest<IgnoreQueryFiltersAnalyzer, DefaultVerifier>
        {
            TestCode = source
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TP0001", DiagnosticSeverity.Error)
                .WithSpan(31, 26, 31, 49)
                .WithArguments("GetAllBad"));

        await test.RunAsync();
    }

    // ------------------------------------------------------------------ //
    // Test 4: attribute with whitespace-only Reason → TP0001 emitted      //
    // ------------------------------------------------------------------ //
    [Fact]
    public async Task IgnoreQueryFilters_WithAttributeWhitespaceReason_EmitsTP0001()
    {
        var source = SharedStubs + """
            namespace SomeApp
            {
                using EfFake;
                using TallerPro.Domain.Common;

                public class SomeService
                {
                    [AllowIgnoreQueryFilters("   ")]
                    public void GetAllWhitespace()
                    {
                        var db = new FakeSet<string>();
                        var result = db.IgnoreQueryFilters();
                    }
                }
            }
            """;

        var test = new CSharpAnalyzerTest<IgnoreQueryFiltersAnalyzer, DefaultVerifier>
        {
            TestCode = source
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TP0001", DiagnosticSeverity.Error)
                .WithSpan(31, 26, 31, 49)
                .WithArguments("GetAllWhitespace"));

        await test.RunAsync();
    }
}
