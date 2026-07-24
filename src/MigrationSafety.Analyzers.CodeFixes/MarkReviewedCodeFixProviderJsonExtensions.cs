using System;
using Microsoft.CodeAnalysis;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Bridges <see cref="MarkReviewedCodeFixProvider"/> to the <see cref="ReviewRecord"/> model:
    /// building the JSON payload embedded in a suppression comment, and recovering it back. All
    /// actual (de)serialization is delegated to <see cref="ReviewRecordSerializer"/> - this class
    /// owns none of the JSON shape or formatting itself.
    /// </summary>
    public static class MarkReviewedCodeFixProviderJsonExtensions
    {
        /// <summary>
        /// Builds the marker comment text for <paramref name="record"/>, suitable for insertion by
        /// <see cref="MarkReviewedCodeFixProvider"/>.
        /// </summary>
        /// <param name="record">The review metadata to embed in the comment.</param>
        /// <returns>Marker text of the form <c>migration-safety:reviewed (RULEID) {json}</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is null.</exception>
        public static string ToMarkerText(this ReviewRecord record) => record switch
        {
            null => throw new ArgumentNullException(nameof(record)),
            _ => SuppressionComment.Marker + " (" + record.RuleId + ") " + ReviewRecordSerializer.Serialize(record),
        };

        /// <summary>
        /// Attempts to recover the <see cref="ReviewRecord"/> embedded in the review marker comment
        /// on the statement containing <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The syntax node whose enclosing statement is inspected.</param>
        /// <param name="record">Receives the recovered <see cref="ReviewRecord"/> on success, or null on failure.</param>
        /// <returns>True if a review record was found and parsed successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
        public static bool TryReadReviewRecord(SyntaxNode node, out ReviewRecord? record)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (SuppressionComment.TryGetReviewedPayload(node, out var payload) &&
                ReviewRecordSerializer.TryDeserialize(payload, out record))
            {
                return true;
            }

            record = null;
            return false;
        }
    }
}
