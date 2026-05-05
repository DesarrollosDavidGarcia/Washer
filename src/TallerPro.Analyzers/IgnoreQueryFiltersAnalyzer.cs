using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TallerPro.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IgnoreQueryFiltersAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.TP0001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check whether the invoked method name is exactly "IgnoreQueryFilters"
        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

        if (methodName != "IgnoreQueryFilters")
        {
            return;
        }

        // Walk up to the containing method declaration
        var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod is null)
        {
            return;
        }

        // Look for AllowIgnoreQueryFilters / AllowIgnoreQueryFiltersAttribute in the method's attribute lists
        foreach (var attrList in containingMethod.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();
                if (attrName is not ("AllowIgnoreQueryFilters" or "AllowIgnoreQueryFiltersAttribute"))
                {
                    continue;
                }

                // Attribute found — check that the first argument is a non-empty, non-whitespace string literal
                var args = attr.ArgumentList?.Arguments;
                if (args is null || args.Value.Count == 0)
                {
                    // Attribute present but no argument → treat as missing reason
                    break;
                }

                var firstArg = args.Value[0].Expression;

                if (firstArg is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var value = literal.Token.ValueText;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return; // valid authorization — no diagnostic
                    }
                }

                // Argument is not a non-whitespace string literal (empty, whitespace, or non-literal)
                break;
            }
        }

        // No valid authorization found → emit TP0001
        var methodNameText = containingMethod.Identifier.Text;
        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.TP0001,
                invocation.GetLocation(),
                methodNameText));
    }
}
