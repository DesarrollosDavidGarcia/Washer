using Microsoft.CodeAnalysis;

namespace TallerPro.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TP0001 = new(
        id: "TP0001",
        title: "Direct tenant ID access without global query filter",
        messageFormat: "Tenant data must be accessed through the scoped DbContext with global query filters enabled",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Bypassing global query filters exposes cross-tenant data leaks.");

    public static readonly DiagnosticDescriptor TP0002 = new(
        id: "TP0002",
        title: "Missing tenant scope in query",
        messageFormat: "Queries on tenant-scoped entities must include tenant filtering",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0003 = new(
        id: "TP0003",
        title: "IgnoreQueryFilters on tenant-scoped entity",
        messageFormat: "IgnoreQueryFilters() is prohibited on tenant-scoped entities",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0004 = new(
        id: "TP0004",
        title: "Raw SQL without tenant filter",
        messageFormat: "Raw SQL queries must include tenant filtering via SESSION_CONTEXT or parameterized TenantId",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0005 = new(
        id: "TP0005",
        title: "Console.WriteLine usage prohibited",
        messageFormat: "Use Serilog instead of Console.WriteLine",
        category: "TallerPro.Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
