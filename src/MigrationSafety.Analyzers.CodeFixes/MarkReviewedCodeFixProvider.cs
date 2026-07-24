using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
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
    ///
    /// The fix is idempotent: re-applying it for a diagnostic id already recorded on a statement's
    /// marker comment is a no-op, and applying it for a second diagnostic id on a statement that
    /// already carries a marker (for example a statement flagged by two rules at once) merges the
    /// new id into the existing comment instead of stacking a duplicate marker underneath it.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MarkReviewedCodeFixProvider))]
    [Shared]
    public sealed class MarkReviewedCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Mark as reviewed (migration-safety)";

        private const string DefaultNote = "TODO: document why this is safe";

        /// <summary>
        /// Matches an existing <c>migration-safety:reviewed</c> single-line comment, capturing the
        /// parenthesised rule-id list (if any) and any free-text note that follows it, so a second
        /// fix on the same statement can merge into the existing comment instead of stacking a new
        /// one underneath it.
        /// </summary>
        private static readonly Regex ExistingMarkerPattern = new Regex(
            @"^//\s*migration\s*-?\s*safety\s*:\s*reviewed\s*(\(\s*(?<ids>[^)]*)\s*\))?\s*(?<note>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            DiagnosticIds.DropColumn,
            DiagnosticIds.NonConcurrentIndex,
            DiagnosticIds.TableRewrite,
            DiagnosticIds.DropTable,
            DiagnosticIds.AddNotNullColumn,
            DiagnosticIds.RenameColumn);

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => MarkReviewedFixAllProvider.Instance;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="context"/> has a default (uninitialized) value.</exception>
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

        private static Task<Document> InsertLeadingCommentAsync(
            Document document,
            SyntaxNode root,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken)
        {
            var newStatement = WithLeadingMarker(statement, diagnosticId);
            var newRoot = root.ReplaceNode(statement, newStatement);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static Task<Document> InsertTrailingCommentAsync(
            Document document,
            SyntaxNode root,
            StatementSyntax statement,
            string diagnosticId,
            CancellationToken cancellationToken)
        {
            var newStatement = WithTrailingMarker(statement, diagnosticId);
            var newRoot = root.ReplaceNode(statement, newStatement);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Returns <paramref name="statement"/> with <paramref name="diagnosticId"/> recorded in its
        /// leading <c>migration-safety:reviewed</c> marker comment, merging into an already-present
        /// marker (from an earlier fix, possibly for a different rule id on the same statement)
        /// instead of stacking a second comment above it. Applying the same id twice is a no-op.
        /// </summary>
        /// <param name="statement">The flagged statement to annotate.</param>
        /// <param name="diagnosticId">The migration-safety rule id being marked as reviewed.</param>
        /// <returns>The statement with an updated leading marker comment.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="diagnosticId"/> is null or empty.</exception>
        internal static StatementSyntax WithLeadingMarker(StatementSyntax statement, string diagnosticId)
        {
            if (statement == null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            if (string.IsNullOrEmpty(diagnosticId))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(diagnosticId));
            }

            var leading = statement.GetLeadingTrivia();

            var existingIndex = FindExistingMarkerIndex(leading);
            if (existingIndex >= 0)
            {
                var merged = MergeMarkerTrivia(leading[existingIndex], diagnosticId, leadingSpace: string.Empty);
                var mergedLeading = leading.Replace(leading[existingIndex], merged);
                return statement.WithLeadingTrivia(mergedLeading);
            }

            var indent = leading.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

            var comment = SyntaxFactory.Comment(BuildMarkerText(new[] { diagnosticId }, DefaultNote, leadingSpace: string.Empty));

            var newLeading = leading
                .Add(comment)
                .Add(SyntaxFactory.ElasticEndOfLine("\n"));

            if (indent.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                newLeading = newLeading.Add(indent);
            }

            return statement.WithLeadingTrivia(newLeading);
        }

        /// <summary>
        /// Returns <paramref name="statement"/> with <paramref name="diagnosticId"/> recorded in its
        /// trailing <c>migration-safety:reviewed</c> marker comment, merging into an already-present
        /// marker instead of stacking a second comment after it. Applying the same id twice is a no-op.
        /// </summary>
        /// <param name="statement">The flagged statement to annotate.</param>
        /// <param name="diagnosticId">The migration-safety rule id being marked as reviewed.</param>
        /// <returns>The statement with an updated trailing marker comment.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="diagnosticId"/> is null or empty.</exception>
        internal static StatementSyntax WithTrailingMarker(StatementSyntax statement, string diagnosticId)
        {
            if (statement == null)
            {
                throw new ArgumentNullException(nameof(statement));
            }

            if (string.IsNullOrEmpty(diagnosticId))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(diagnosticId));
            }

            var trailing = statement.GetTrailingTrivia();

            var existingIndex = FindExistingMarkerIndex(trailing);
            if (existingIndex >= 0)
            {
                var merged = MergeMarkerTrivia(trailing[existingIndex], diagnosticId, leadingSpace: " ");
                var mergedTrailing = trailing.Replace(trailing[existingIndex], merged);
                return statement.WithTrailingTrivia(mergedTrailing);
            }

            var comment = SyntaxFactory.Comment(BuildMarkerText(new[] { diagnosticId }, DefaultNote, leadingSpace: " "));

            var newTrailing = trailing.Add(comment);

            if (!newTrailing.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                newTrailing = newTrailing.Add(SyntaxFactory.ElasticEndOfLine("\n"));
            }

            return statement.WithTrailingTrivia(newTrailing);
        }

        /// <summary>
        /// Finds the index of a leading/trailing trivia entry that already carries a
        /// <c>migration-safety:reviewed</c> single-line comment, or -1 if the list has none.
        /// </summary>
        private static int FindExistingMarkerIndex(SyntaxTriviaList triviaList)
        {
            for (var i = 0; i < triviaList.Count; i++)
            {
                var trivia = triviaList[i];
                if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    continue;
                }

                if (ExistingMarkerPattern.IsMatch(trivia.ToString().Trim()))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Merges <paramref name="diagnosticId"/> into an existing marker comment trivia, producing
        /// a single comment that lists every rule id reviewed on that statement instead of stacking
        /// a duplicate marker underneath it. Re-applying the same diagnostic id is a no-op that
        /// yields byte-identical trivia.
        /// </summary>
        private static SyntaxTrivia MergeMarkerTrivia(SyntaxTrivia existing, string diagnosticId, string leadingSpace)
        {
            var match = ExistingMarkerPattern.Match(existing.ToString().Trim());
            var existingIds = match.Groups["ids"].Success
                ? match.Groups["ids"].Value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => id.Length > 0)
                : Enumerable.Empty<string>();

            var mergedIds = existingIds
                .Concat(new[] { diagnosticId })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var note = match.Groups["note"].Success && match.Groups["note"].Value.Trim().Length > 0
                ? match.Groups["note"].Value.Trim()
                : DefaultNote;

            var text = BuildMarkerText(mergedIds, note, leadingSpace);
            return SyntaxFactory.Comment(text);
        }

        /// <summary>
        /// Builds the literal text of a <c>migration-safety:reviewed</c> single-line comment for the
        /// given set of rule ids and trailing note.
        /// </summary>
        private static string BuildMarkerText(IReadOnlyCollection<string> ruleIds, string note, string leadingSpace)
        {
            var idsText = string.Join(", ", ruleIds);
            return leadingSpace + "// " + SuppressionComment.Marker + " (" + idsText + ") " + note;
        }
    }

    /// <summary>
    /// Applies <see cref="MarkReviewedCodeFixProvider"/> across every flagged statement in a
    /// document, project, or solution as a single batch. Diagnostics are folded into the syntax
    /// tree one statement at a time - tracking each affected statement via
    /// <see cref="SyntaxNode.TrackNodes(SyntaxNode[])"/> so it keeps a stable handle across edits
    /// made to earlier statements - so a statement carrying two diagnostics (for example an
    /// <c>AlterColumn</c> call flagged by both MIG003 and another rule) gets one merged marker
    /// comment instead of two conflicting text edits landing on the same span.
    /// </summary>
    internal sealed class MarkReviewedFixAllProvider : FixAllProvider
    {
        private const string TrailingEquivalenceKey = "Mark as reviewed (migration-safety).Trailing";

        /// <summary>
        /// The single shared instance of this provider, as recommended for stateless
        /// <see cref="FixAllProvider"/> implementations.
        /// </summary>
        public static readonly MarkReviewedFixAllProvider Instance = new MarkReviewedFixAllProvider();

        private MarkReviewedFixAllProvider()
        {
        }

        /// <summary>
        /// Builds the single "Fix all" code action for the requested scope.
        /// </summary>
        /// <param name="fixAllContext">The context describing the scope (document, project, or solution) to fix.</param>
        /// <returns>A code action that applies the batched fix, or <see langword="null"/> if there is nothing to do.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fixAllContext"/> has a default (uninitialized) value.</exception>
        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var documents = await GetDocumentsAsync(fixAllContext).ConfigureAwait(false);
            if (documents.IsEmpty)
            {
                return null;
            }

            var insertAsLeading = !string.Equals(fixAllContext.CodeActionEquivalenceKey, TrailingEquivalenceKey, StringComparison.Ordinal);

            return CodeAction.Create(
                title: "Mark all as reviewed (migration-safety)",
                createChangedSolution: ct => FixSolutionAsync(fixAllContext, documents, insertAsLeading, ct),
                equivalenceKey: nameof(MarkReviewedFixAllProvider));
        }

        /// <summary>
        /// Resolves the set of documents in scope for the requested fix-all operation.
        /// </summary>
        private static Task<ImmutableArray<Document>> GetDocumentsAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(fixAllContext.Document != null
                        ? ImmutableArray.Create(fixAllContext.Document)
                        : ImmutableArray<Document>.Empty);

                case FixAllScope.Project:
                    return Task.FromResult(fixAllContext.Project.Documents.ToImmutableArray());

                case FixAllScope.Solution:
                    var builder = ImmutableArray.CreateBuilder<Document>();
                    foreach (var project in fixAllContext.Solution.Projects)
                    {
                        builder.AddRange(project.Documents);
                    }

                    return Task.FromResult(builder.ToImmutable());

                default:
                    return Task.FromResult(ImmutableArray<Document>.Empty);
            }
        }

        /// <summary>
        /// Fixes every in-scope document and returns the resulting solution.
        /// </summary>
        private static async Task<Solution> FixSolutionAsync(
            FixAllContext fixAllContext,
            ImmutableArray<Document> documents,
            bool insertAsLeading,
            CancellationToken cancellationToken)
        {
            var solution = fixAllContext.Solution;

            foreach (var document in documents)
            {
                var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                if (diagnostics.IsDefaultOrEmpty)
                {
                    continue;
                }

                var fixedRoot = await FixDocumentAsync(document, diagnostics, insertAsLeading, cancellationToken).ConfigureAwait(false);
                if (fixedRoot == null)
                {
                    continue;
                }

                solution = solution.WithDocumentSyntaxRoot(document.Id, fixedRoot);
            }

            return solution;
        }

        /// <summary>
        /// Folds every flagged diagnostic in a single document into one merged root, applying the
        /// marker to each distinct statement exactly once regardless of how many diagnostics on it
        /// were selected for the batch.
        /// </summary>
        private static async Task<SyntaxNode?> FixDocumentAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            bool insertAsLeading,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return null;
            }

            var statementToDiagnosticIds = new Dictionary<StatementSyntax, List<string>>();
            var orderedStatements = new List<StatementSyntax>();

            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                var statement = node.FirstAncestorOrSelf<StatementSyntax>();
                if (statement == null)
                {
                    continue;
                }

                if (!statementToDiagnosticIds.TryGetValue(statement, out var ids))
                {
                    ids = new List<string>();
                    statementToDiagnosticIds[statement] = ids;
                    orderedStatements.Add(statement);
                }

                if (!ids.Contains(diagnostic.Id, StringComparer.OrdinalIgnoreCase))
                {
                    ids.Add(diagnostic.Id);
                }
            }

            if (orderedStatements.Count == 0)
            {
                return root;
            }

            // Track every affected statement up front so it can be relocated in the tree after
            // earlier statements in the same document have already been rewritten.
            var trackedRoot = root.TrackNodes(orderedStatements);

            foreach (var originalStatement in orderedStatements)
            {
                var currentStatement = trackedRoot.GetCurrentNode(originalStatement);
                if (currentStatement == null)
                {
                    continue;
                }

                var ids = statementToDiagnosticIds[originalStatement];
                var updatedStatement = currentStatement;
                foreach (var diagnosticId in ids)
                {
                    updatedStatement = insertAsLeading
                        ? MarkReviewedCodeFixProvider.WithLeadingMarker(updatedStatement, diagnosticId)
                        : MarkReviewedCodeFixProvider.WithTrailingMarker(updatedStatement, diagnosticId);
                }

                trackedRoot = trackedRoot.ReplaceNode(currentStatement, updatedStatement);
            }

            return trackedRoot;
        }
    }
}
