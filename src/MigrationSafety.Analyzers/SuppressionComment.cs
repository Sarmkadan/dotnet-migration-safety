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
    /// <item><c>// migration-safety:reviewed</c> followed by any text</item>
    /// <item><c>/* migration-safety:reviewed ... */</c> containing any text</item>
    /// <item>Any whitespace variations around colons, parentheses, and keywords</item>
    /// <item>Trailing text such as reviewer names, dates, or additional notes</item>
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
        /// <summary>
        /// The suppression marker text that must appear in the comment.
        /// </summary>
        public const string Marker = "migration-safety:reviewed";

        // Case-insensitive, whitespace-tolerant regex pattern for the marker
        // Allows: optional whitespace around colon, optional parentheses with content, any trailing text
        private static readonly Regex MarkerPattern = new Regex(
            @"migration\s*-?\s*safety\s*:\s*reviewed\s*(\([^)]*\))?\s*(.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Determines whether the specified syntax node has a leading comment containing the
        /// migration-safety review marker.
        /// </summary>
        /// <param name="node">The syntax node to check for a suppression comment.</param>
        /// <returns>
        /// <see langword="true" /> if the node has a leading comment containing the
        /// migration-safety:reviewed marker; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="node" /> is <see langword="null" />.
        /// </exception>
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

                // Validate input length to prevent memory exhaustion attacks
                if (content.Length > InputValidation.MaxCommentLength)
                {
                    return false;
                }

                // Check if the content matches the marker pattern (case-insensitive, whitespace-tolerant)
                try
                {
                    if (MarkerPattern.IsMatch(content))
                    {
                        return true;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Timeout occurred during matching - treat as no match
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Recovers the free-text payload that follows the <c>migration-safety:reviewed</c> marker
        /// (and its optional parenthesised rule-id list) in the leading comment trivia of the
        /// statement containing <paramref name="node"/>. Used by consumers that embed structured
        /// review metadata - such as a serialized review record - in that trailing text.
        /// </summary>
        /// <param name="node">The syntax node whose enclosing statement is inspected for a review marker comment.</param>
        /// <param name="payload">Receives the trimmed trailing text on success, or <see langword="null" /> on failure.</param>
        /// <returns>
        /// <see langword="true" /> if a review marker comment was found; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="node" /> is <see langword="null" />.
        /// </exception>
        public static bool TryGetReviewedPayload(SyntaxNode node, out string? payload)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var statement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
            {
                payload = null;
                return false;
            }

            foreach (var trivia in statement.GetLeadingTrivia())
            {
                if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                    !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    continue;
                }

                var content = ExtractCommentContent(trivia.ToString());

                // Validate input length to prevent memory exhaustion attacks
                if (content.Length > InputValidation.MaxCommentLength)
                {
                    payload = null;
                    return false;
                }

                try
                {
                    var match = MarkerPattern.Match(content);
                    if (match.Success)
                    {
                        payload = match.Groups[1].Value.Trim();
                        return true;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Timeout occurred during matching - treat as no match
                    payload = null;
                    return false;
                }
            }

            payload = null;
            return false;
        }

        /// <summary>
        /// Extracts the actual comment content from comment trivia, handling both single-line
        /// and multi-line comment syntax.
        /// </summary>
        /// <param name="commentText">The full comment text including // or /* */ markers.</param>
        /// <returns>The extracted content without comment delimiters.</returns>
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

        /// <summary>
        /// Constants for input validation to prevent ReDoS and excessive memory usage.
        /// </summary>
        private static class InputValidation
        {
            /// <summary>
            /// Maximum allowed length for comment content to prevent memory exhaustion attacks.
            /// </summary>
            public const int MaxCommentLength = 1024 * 1024; // 1 MB
        }
    }
}
