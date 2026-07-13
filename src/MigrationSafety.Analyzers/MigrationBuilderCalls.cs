using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Small helpers shared by the analyzer and code fix for reading arguments off a
    /// <c>migrationBuilder.Xxx(...)</c> call. The methods stay tolerant of both named and
    /// positional arguments so hand-edited migrations are handled the same as generated ones.
    /// </summary>
    internal static class MigrationBuilderCalls
    {
        internal const string MigrationBuilderTypeName = "MigrationBuilder";

        /// <summary>
        /// True when <paramref name="method"/> is an instance method declared on
        /// EF Core's MigrationBuilder (matched by simple type name so a stub type works too).
        /// </summary>
        internal static bool IsMigrationBuilderMethod(IMethodSymbol method)
        {
            var containing = method?.ContainingType;
            return containing != null && containing.Name == MigrationBuilderTypeName;
        }

        /// <summary>
        /// Resolves the string literal passed for the parameter called <paramref name="parameterName"/>,
        /// whether it was supplied positionally or by name. Returns null when the value is
        /// not a plain string literal (for example an interpolated string or a variable).
        /// </summary>
        internal static string GetStringArgument(
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            string parameterName)
        {
            var parameter = method.Parameters.FirstOrDefault(p => p.Name == parameterName);
            if (parameter == null)
            {
                return null;
            }

            var arguments = invocation.ArgumentList.Arguments;

            // Named argument wins if present.
            foreach (var argument in arguments)
            {
                if (argument.NameColon != null &&
                    argument.NameColon.Name.Identifier.ValueText == parameterName)
                {
                    return AsLiteral(argument.Expression);
                }
            }

            // Otherwise fall back to positional binding (skip if any name colon reorders things).
            int index = parameter.Ordinal;
            if (index < arguments.Count && arguments[index].NameColon == null)
            {
                return AsLiteral(arguments[index].Expression);
            }

            return null;
        }

        private static string AsLiteral(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax literal
                ? literal.Token.ValueText
                : null;
        }
    }
}
