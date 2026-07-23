using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MigrationSafety.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MigrationSafetyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly HashSet<string> DangerousOperationNames = new HashSet<string>
        {
            "DropColumn",
            "NonConcurrentIndex",
            "TableRewrite",
            "DropTable",
            "AddNotNullColumn",
            "RenameColumn"
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticIds.DropColumn,
            DiagnosticIds.NonConcurrentIndex,
            DiagnosticIds.TableRewrite,
            DiagnosticIds.DropTable,
            DiagnosticIds.AddNotNullColumn,
            DiagnosticIds.RenameColumn);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Node);
            ArgumentNullException.ThrowIfNull(context.SemanticModel);

            if (context.Node is InvocationExpressionSyntax invocation)
            {
                if (DangerousOperationNames.Contains(invocation.Expression.ToString()))
                {
                    try
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
                        if (symbol != null)
                        {
                            // Rest of the method implementation...
                        }
                    }
                    catch (Exception ex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticIds.UnexpectedError, ex));
                    }
                }
            }
        }
    }
}
