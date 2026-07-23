using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        /// Well-known annotation names used by database providers to indicate online/concurrent operations.
        /// </summary>
        private static readonly HashSet<string> OnlineIndexAnnotations = new HashSet<string>(StringComparer.Ordinal)
        {
            WellKnownAnnotations.NpgsqlCreateIndexConcurrently,
            WellKnownAnnotations.SqlServerCreateIndexOnline,
            WellKnownAnnotations.SqlServerCreateIndexAlgorithm,
            WellKnownAnnotations.NpgsqlCreateIndexAlgorithm,
            WellKnownAnnotations.MySqlCreateIndexOnline
        };

        /// <summary>
        /// Checks if a CreateIndex invocation has an online/concurrent annotation that makes it safe.
        /// </summary>
        /// <param name="invocation">The CreateIndex invocation expression.</param>
        /// <param name="semanticModel">The semantic model to resolve symbols.</param>
        /// <returns>True if the invocation has an online annotation; false otherwise.</returns>
        internal static bool HasOnlineIndexAnnotation(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            // Walk the invocation chain to find any .Annotation() calls
            var current = invocation.Parent;
            while (current != null)
            {
                if (current is InvocationExpressionSyntax parentInvocation
                    && parentInvocation.Expression is MemberAccessExpressionSyntax parentMemberAccess
                    && parentMemberAccess.Name.Identifier.Text == "Annotation")
                {
                    // Check if this is an online/concurrent index annotation
                    if (IsOnlineIndexAnnotation(parentInvocation, semanticModel))
                    {
                        return true;
                    }
                }

                current = current.Parent;
            }

            return false;
        }

        /// <summary>
        /// Checks if an Annotation invocation is specifically for online/concurrent index operations.
        /// </summary>
        private static bool IsOnlineIndexAnnotation(
            InvocationExpressionSyntax annotationInvocation,
            SemanticModel semanticModel)
        {
            if (!(annotationInvocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            // Get the annotation name argument
            var nameArg = GetStringArgument(annotationInvocation, "name", semanticModel);
            if (nameArg == null)
            {
                return false;
            }

            // Check if it's one of our known online index annotations
            return OnlineIndexAnnotations.Contains(nameArg);
        }

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
            int index = method.Parameters.FirstOrDefault(p => p.Name == parameterName)?.Ordinal ?? -1;
            if (index >= 0 && index < arguments.Count && arguments[index].NameColon == null)
            {
                return AsLiteral(arguments[index].Expression);
            }

            return null;
        }

        /// <summary>
        /// Resolves the string literal passed for the parameter called <paramref name="parameterName"/>,
        /// whether it was supplied positionally or by name. Returns null when the value is
        /// not a plain string literal (for example an interpolated string or a variable).
        /// This version accepts a SemanticModel to look up the method symbol.
        /// </summary>
        internal static string GetStringArgument(
            InvocationExpressionSyntax invocation,
            string parameterName,
            SemanticModel semanticModel)
        {
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            return methodSymbol != null ? GetStringArgument(invocation, methodSymbol, parameterName) : null;
        }

        private static string AsLiteral(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax literal
                ? literal.Token.ValueText
                : null;
        }

        internal static ArgumentSyntax GetNamedArgument(
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            string parameterName)
        {
            var arguments = invocation.ArgumentList.Arguments;

            foreach (var argument in arguments)
            {
                if (argument.NameColon != null &&
                    argument.NameColon.Name.Identifier.ValueText == parameterName)
                {
                    return argument;
                }
            }

            return null;
        }
    }
}
