using System;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Immutable record of a human review decision for a migration-safety diagnostic. Instances
    /// are embedded (as JSON, via <c>ReviewRecordSerializer</c>) in the suppression comment that
    /// <see cref="MarkReviewedCodeFixProvider"/> inserts, and can equally be emitted to an external
    /// audit report.
    /// </summary>
    public sealed class ReviewRecord : IEquatable<ReviewRecord>
    {
        /// <summary>
        /// The schema version of this record, bumped whenever the shape of <see cref="ReviewRecord"/>
        /// changes in a way that is not backward compatible. Older comments keep the version they
        /// were written with, so the serializer can still parse them.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Initializes a new <see cref="ReviewRecord"/>.
        /// </summary>
        /// <param name="ruleId">The migration-safety diagnostic id that was reviewed (for example <c>MIG001</c>).</param>
        /// <param name="reviewer">The identity of the person who performed the review.</param>
        /// <param name="dateUtc">The UTC date and time the review was performed.</param>
        /// <param name="migrationFile">The path of the migration file the review applies to.</param>
        /// <param name="schemaVersion">The schema version this record was created under. Defaults to <see cref="CurrentSchemaVersion"/>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ruleId"/>, <paramref name="reviewer"/>, or <paramref name="migrationFile"/> is null or empty.</exception>
        public ReviewRecord(string ruleId, string reviewer, DateTime dateUtc, string migrationFile, int schemaVersion = CurrentSchemaVersion)
        {
            ThrowIfNullOrEmpty(ruleId, nameof(ruleId));
            ThrowIfNullOrEmpty(reviewer, nameof(reviewer));
            ThrowIfNullOrEmpty(migrationFile, nameof(migrationFile));

            RuleId = ruleId;
            Reviewer = reviewer;
            DateUtc = dateUtc.Kind == DateTimeKind.Utc ? dateUtc : dateUtc.ToUniversalTime();
            MigrationFile = migrationFile;
            SchemaVersion = schemaVersion;
        }

        /// <summary>The migration-safety diagnostic id that was reviewed (for example <c>MIG001</c>).</summary>
        public string RuleId { get; }

        /// <summary>The identity of the person who performed the review.</summary>
        public string Reviewer { get; }

        /// <summary>The UTC date and time the review was performed.</summary>
        public DateTime DateUtc { get; }

        /// <summary>The path of the migration file the review applies to.</summary>
        public string MigrationFile { get; }

        /// <summary>The schema version this record was created under.</summary>
        public int SchemaVersion { get; }

        /// <inheritdoc />
        public bool Equals(ReviewRecord? other) => other switch
        {
            null => false,
            _ => ReferenceEquals(this, other) ||
                 (RuleId == other.RuleId &&
                  Reviewer == other.Reviewer &&
                  DateUtc.Equals(other.DateUtc) &&
                  MigrationFile == other.MigrationFile &&
                  SchemaVersion == other.SchemaVersion)
        };

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as ReviewRecord);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + RuleId.GetHashCode();
                hash = (hash * 31) + Reviewer.GetHashCode();
                hash = (hash * 31) + DateUtc.GetHashCode();
                hash = (hash * 31) + MigrationFile.GetHashCode();
                hash = (hash * 31) + SchemaVersion.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Validates that a string argument is neither null nor empty.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">The name of the parameter being validated, used in the thrown exception.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or empty.</exception>
        private static void ThrowIfNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", paramName);
            }
        }
    }
}
