using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// The analyzer honours an inline opt-out so a human who has genuinely reviewed a risky
    /// operation can silence it without a global suppression. The marker lives as a leading
    /// comment on the statement that contains the call.
    ///
    /// The parser is intentionally tolerant of formatting drift to survive code formatting tools
    /// (dotnet format, Rider), extra whitespace, comment style changes, or hand-edited comments.
    ///
    /// Accepted grammar (case-insensitive, whitespace-tolerant):
    /// <list type="bullet">
    ///   <item><c>// migration-safety:reviewed</c> followed by any text</item>
    ///   <item><c>/* migration-safety:reviewed ... */</c> containing any text</item>
    ///   <item>Any whitespace variations around colons, parentheses, and keywords</item>
    ///   <item>Trailing text such as reviewer names, dates, or additional notes</item>
    /// </list>
    ///
    /// Examples of accepted formats:
    /// <code>
    /// // migration-safety:reviewed safe to drop
    /// // migration-safety : reviewed safe to drop
    /// // migration-safety:reviewed (MIG001) TODO: document why this is safe
    /// /* migration-safety:reviewed */
    /// /* MIGRATION-SAFETY : REVIEWED (mig001) */
    /// // migration-safety:reviewed reviewed by Alice on 2024-01-15
    /// </code>
    /// </summary>
    public static class SuppressionComment
    {
        public const string Marker = "migration-safety:reviewed";

        // Case-insensitive, whitespace-tolerant regex pattern for the marker
        // Allows: optional whitespace around colon, optional parentheses with content, any trailing text
        private static readonly Regex MarkerPattern = new Regex(
            @"migration\s*-?\s*safety\s*:\s*reviewed\s*(\([^)]*\))?\s*(.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsReviewed(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var statement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
            {
                return false;
            }

            foreach (var trivia in statement.GetLeadingTrivia())
            {
                if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                    !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    continue;
                }

                var commentText = trivia.ToString();

                // Extract comment content (remove leading // or /* and trailing */)
                string content = ExtractCommentContent(commentText);

                // Check if the content matches the marker pattern (case-insensitive, whitespace-tolerant)
                if (MarkerPattern.IsMatch(content))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts the actual comment content from comment trivia, handling both single-line
        /// and multi-line comment syntax.
        /// </summary>
        /// <param name="commentText">The full comment text including // or /* */ markers</param>
        /// <returns>The extracted content without comment delimiters</returns>
        private static string ExtractCommentContent(string commentText)
        {
            if (string.IsNullOrEmpty(commentText))
            {
                return commentText;
            }

            // Handle single-line comments: // content
            if (commentText.StartsWith("//", StringComparison.Ordinal))
            {
                return commentText.Substring(2).Trim();
            }

            // Handle multi-line comments: /* content */
            if (commentText.StartsWith("/*", StringComparison.Ordinal) &&
                commentText.EndsWith("*/", StringComparison.Ordinal))
            {
                return commentText.Substring(2, commentText.Length - 4).Trim();
            }

            // Fallback: return as-is
            return commentText.Trim();
        }
    }
}