using Microsoft.CodeAnalysis;

namespace TallerPro.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TP0001 = new(
        id: "TP0001",
        title: "IgnoreQueryFilters() requiere autorización explícita",
        messageFormat: "Llamada a IgnoreQueryFilters() en método '{0}' sin [AllowIgnoreQueryFilters] con razón no vacía",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IgnoreQueryFilters() omite los global query filters de tenant — use [AllowIgnoreQueryFilters(\"razón\")] con razón documentada.");

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
        title: "NovitaAi call without PII masking",
        messageFormat: "Calls to NovitaAiClient must apply PiiMasker to the payload before sending",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Sending unmasked PII to external LLM APIs violates constitution §PII masking dual.");

    public static readonly DiagnosticDescriptor TP0005 = new(
        id: "TP0005",
        title: "Console.WriteLine usage prohibited",
        messageFormat: "Use Serilog instead of Console.WriteLine",
        category: "TallerPro.Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
