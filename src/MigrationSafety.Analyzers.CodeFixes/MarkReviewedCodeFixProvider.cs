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
            DiagnosticIds.DropTable,
            DiagnosticIds.AddNotNullColumn,
            DiagnosticIds.RenameColumn);

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

            // Register both leading and trailing comment fixes
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title + " (leading comment)",
                    createChangedDocument: ct => MarkReviewedAsync(context.Document, statement, diagnostic.Id, ct, insertAsLeading: true),
                    equivalenceKey: Title + ".Leading"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title + " (trailing comment)",
                    createChangedDocument: ct => MarkReviewedAsync(context.Document, statement, diagnostic.Id, ct, insertAsLeading: false),
                    equivalenceKey: Title + ".Trailing"),
                diagnostic);
        }

        private static async Task<Document> MarkReviewedAsync(
            Document document,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken,
            bool insertAsLeading = true)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            if (insertAsLeading)
            {
                return await InsertLeadingCommentAsync(document, root, statement, diagnosticId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await InsertTrailingCommentAsync(document, root, statement, diagnosticId, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<Document> InsertLeadingCommentAsync(
            Document document,
            SyntaxNode root,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken)
        {
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

        private static async Task<Document> InsertTrailingCommentAsync(
            Document document,
            SyntaxNode root,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken)
        {
            // Get the trailing trivia of the statement
            var trailing = statement.GetTrailingTrivia();

            // Create the comment with proper spacing
            var comment = SyntaxFactory.Comment(" // " + SuppressionComment.Marker + " (" + diagnosticId + ") TODO: document why this is safe");

            // Add the comment to the trailing trivia
            var newTrailing = trailing.Add(comment);

            // Ensure there's a newline after the statement
            if (!newTrailing.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                newTrailing = newTrailing.Add(SyntaxFactory.ElasticEndOfLine("\n"));
            }

            var newStatement = statement.WithTrailingTrivia(newTrailing);
            var newRoot = root.ReplaceNode(statement, newStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}