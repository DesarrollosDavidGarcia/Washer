using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TallerPro.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TallerProAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.TP0001,
            DiagnosticDescriptors.TP0002,
            DiagnosticDescriptors.TP0003,
            DiagnosticDescriptors.TP0004,
            DiagnosticDescriptors.TP0005);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // TP0005: Console.WriteLine stub — lógica real de TP0001-TP0004 en spec posterior
        context.RegisterSyntaxNodeAction(AnalyzeConsoleWriteLine, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeConsoleWriteLine(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "WriteLine",
                Expression: IdentifierNameSyntax { Identifier.Text: "Console" }
            })
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DiagnosticDescriptors.TP0005, invocation.GetLocation()));
        }
    }
}
