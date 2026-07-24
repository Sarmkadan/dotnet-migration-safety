using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Covers the merge/idempotency behavior of <see cref="MarkReviewedCodeFixProvider"/>: a
    /// statement flagged by two diagnostics ends up with one merged marker comment, re-applying
    /// the same diagnostic id twice is a no-op, and "Fix all" across many flagged statements in one
    /// document does not produce conflicting text edits.
    /// </summary>
    public class MarkReviewedMergeTests
    {
        private static StatementSyntax ParseStatement(string statementText)
        {
            var tree = CSharpSyntaxTree.ParseText(
                "class C { void M() { " + statementText + " } }");
            return tree.GetRoot()
                .DescendantNodes()
                .OfType<StatementSyntax>()
                .First(s => s is ExpressionStatementSyntax);
        }

        /// <summary>
        /// A statement flagged by two different rules (e.g. an <c>AlterColumn</c> call caught by
        /// both MIG003 and another rule) gets one merged marker comment listing both rule ids,
        /// instead of two stacked comments.
        /// </summary>
        [Fact]
        public void Same_statement_flagged_by_two_diagnostics_merges_into_one_comment()
        {
            var statement = ParseStatement("migrationBuilder.AlterColumn(name: \"Status\");");

            var afterFirst = MarkReviewedCodeFixProvider.WithLeadingMarker(statement, "MIG003");
            var afterSecond = MarkReviewedCodeFixProvider.WithLeadingMarker(afterFirst, "MIG001");

            var text = afterSecond.ToFullString();

            Assert.Contains("migration-safety:reviewed (MIG001, MIG003)", text);
            Assert.Equal(1, text.Split(new[] { "migration-safety:reviewed" }, System.StringSplitOptions.None).Length - 1);
        }

        /// <summary>
        /// Applying the leading-comment fix twice for the same diagnostic id produces byte-identical
        /// trivia both times: the second application recognizes the existing marker and is a no-op.
        /// </summary>
        [Fact]
        public void Reapplying_the_same_diagnostic_twice_is_idempotent()
        {
            var statement = ParseStatement("migrationBuilder.DropColumn(name: \"Notes\");");

            var afterFirst = MarkReviewedCodeFixProvider.WithLeadingMarker(statement, "MIG001");
            var afterSecond = MarkReviewedCodeFixProvider.WithLeadingMarker(afterFirst, "MIG001");

            Assert.Equal(afterFirst.ToFullString(), afterSecond.ToFullString());
        }

        /// <summary>
        /// The same idempotency guarantee holds for the trailing-comment variant of the fix.
        /// </summary>
        [Fact]
        public void Reapplying_the_same_diagnostic_twice_is_idempotent_for_trailing_marker()
        {
            var statement = ParseStatement("migrationBuilder.DropColumn(name: \"Notes\");");

            var afterFirst = MarkReviewedCodeFixProvider.WithTrailingMarker(statement, "MIG001");
            var afterSecond = MarkReviewedCodeFixProvider.WithTrailingMarker(afterFirst, "MIG001");

            Assert.Equal(afterFirst.ToFullString(), afterSecond.ToFullString());
        }

        /// <summary>
        /// Folding markers into 20 flagged statements in one pass - the same node-tracking strategy
        /// <see cref="MarkReviewedFixAllProvider"/> uses for "Fix all in document" - annotates every
        /// statement with its own correct marker and none of them corrupt a neighboring statement's
        /// text, the failure mode a naive text-based batch fix (recomputing spans against a stale
        /// root) would hit once earlier statements have already grown a leading comment.
        /// </summary>
        [Fact]
        public void Folding_markers_into_twenty_statements_does_not_corrupt_neighbors()
        {
            const int callCount = 20;

            var callLines = string.Join(
                "\n        ",
                Enumerable.Range(0, callCount).Select(i => $"migrationBuilder.DropColumn(name: \"Col{i}\", table: \"Orders\");"));

            var source = @"
class SomeMigration
{
    void Up(object migrationBuilder)
    {
        " + callLines + @"
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();
            var statements = root.DescendantNodes().OfType<ExpressionStatementSyntax>().Cast<StatementSyntax>().ToList();
            Assert.Equal(callCount, statements.Count);

            var trackedRoot = root.TrackNodes(statements);
            foreach (var original in statements)
            {
                var current = trackedRoot.GetCurrentNode(original);
                Assert.NotNull(current);
                var updated = MarkReviewedCodeFixProvider.WithLeadingMarker(current!, "MIG001");
                trackedRoot = trackedRoot.ReplaceNode(current!, updated);
            }

            var finalStatements = trackedRoot.DescendantNodes().OfType<ExpressionStatementSyntax>().ToList();
            Assert.Equal(callCount, finalStatements.Count);

            for (var i = 0; i < callCount; i++)
            {
                var statementText = finalStatements[i].ToFullString();
                Assert.Contains("migration-safety:reviewed (MIG001)", statementText);
                Assert.Contains($"DropColumn(name: \"Col{i}\", table: \"Orders\")", statementText);

                // Each statement carries exactly one marker comment - no duplication, no bleed
                // from a neighboring statement's edit.
                Assert.Equal(1, statementText.Split(new[] { "migration-safety:reviewed" }, System.StringSplitOptions.None).Length - 1);
            }
        }
    }
}
