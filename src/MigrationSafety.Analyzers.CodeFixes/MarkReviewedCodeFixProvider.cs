using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Offers a single, deliberately conservative fix for every migration-safety diagnostic:
    /// prepend the <c>// migration-safety:reviewed</c> marker to the flagged statement. The fix
    /// does not try to rewrite the migration - that decision belongs to a human - it just records
    /// that the risk was seen and accepted, which is exactly what the analyzer then honours.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MarkReviewedCodeFixProvider))]
    [Shared]
    public sealed class MarkReviewedCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Mark as reviewed (migration-safety)";

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                DiagnosticIds.DropColumn,
                DiagnosticIds.NonConcurrentIndex,
                DiagnosticIds.TableRewrite,
                DiagnosticIds.DropTable);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var statement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct => MarkReviewedAsync(context.Document, statement, diagnostic.Id, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> MarkReviewedAsync(
            Document document,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var leading = statement.GetLeadingTrivia();
            var indent = leading.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

            var comment = SyntaxFactory.Comment(
                "// " + SuppressionComment.Marker + " (" + diagnosticId + ") TODO: document why this is safe");

            var newLeading = leading
                .Add(comment)
                .Add(SyntaxFactory.ElasticEndOfLine("\n"));

            if (indent.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                newLeading = newLeading.Add(indent);
            }

            var newStatement = statement.WithLeadingTrivia(newLeading);
            var newRoot = root.ReplaceNode(statement, newStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
