using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// The analyzer honours an inline opt-out so a human who has genuinely reviewed a risky
    /// operation can silence it without a global suppression. The marker lives as a leading
    /// comment on the statement that contains the call:
    /// <code>// migration-safety:reviewed &lt;reason&gt;</code>
    /// </summary>
    public static class SuppressionComment
    {
        public const string Marker = "migration-safety:reviewed";

        public static bool IsReviewed(SyntaxNode node)
        {
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

                if (trivia.ToString().IndexOf(Marker, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
