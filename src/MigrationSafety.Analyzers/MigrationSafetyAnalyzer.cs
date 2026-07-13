using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Flags destructive or lock-heavy operations inside EF Core migrations before they reach
    /// production. Every rule inspects <c>migrationBuilder.*</c> invocations in the syntax tree
    /// and reports on the method-name token so the squiggle lands on the operation itself.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MigrationSafetyAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                Descriptors.DropColumn,
                Descriptors.NonConcurrentIndex,
                Descriptors.TableRewrite,
                Descriptors.DropTable);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(
                GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method))
            {
                return;
            }

            if (!MigrationBuilderCalls.IsMigrationBuilderMethod(method))
            {
                return;
            }

            if (SuppressionComment.IsReviewed(invocation))
            {
                return;
            }

            var reportLocation = memberAccess.Name.GetLocation();

            switch (method.Name)
            {
                case "DropColumn":
                    Report(context, invocation, method, Descriptors.DropColumn, reportLocation, "name", "table");
                    break;

                case "DropTable":
                    ReportSingle(context, invocation, method, Descriptors.DropTable, reportLocation, "name");
                    break;

                case "CreateIndex":
                    Report(context, invocation, method, Descriptors.NonConcurrentIndex, reportLocation, "name", "table");
                    break;

                case "AlterColumn":
                    Report(context, invocation, method, Descriptors.TableRewrite, reportLocation, "name", "table");
                    break;
            }
        }

        private static void Report(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            DiagnosticDescriptor descriptor,
            Location location,
            string firstArg,
            string secondArg)
        {
            var first = MigrationBuilderCalls.GetStringArgument(invocation, method, firstArg) ?? "?";
            var second = MigrationBuilderCalls.GetStringArgument(invocation, method, secondArg) ?? "?";
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, first, second));
        }

        private static void ReportSingle(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            DiagnosticDescriptor descriptor,
            Location location,
            string firstArg)
        {
            var first = MigrationBuilderCalls.GetStringArgument(invocation, method, firstArg) ?? "?";
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, first));
        }
    }
}
