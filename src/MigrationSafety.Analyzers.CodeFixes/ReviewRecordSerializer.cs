using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Serializes and deserializes <see cref="ReviewRecord"/> instances to and from JSON.
    /// </summary>
    /// <remarks>
    /// Dates are always written and read in the round-trippable, culture-invariant
    /// <c>"O"</c> format (<see cref="DateTime.ToString(string, IFormatProvider)"/> with
    /// <see cref="CultureInfo.InvariantCulture"/>), so a machine whose OS culture is, for
    /// example, <c>bg-BG</c> can never write a locale-formatted date such as
    /// <c>22.07.2026</c> into a suppression comment or report. Property casing is fixed to
    /// camelCase regardless of host culture or reflection order. Every payload carries a
    /// <c>schemaVersion</c> field so comments written under an older shape of
    /// <see cref="ReviewRecord"/> still deserialize after the model evolves.
    /// </remarks>
    public static class ReviewRecordSerializer
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Serializes a <see cref="ReviewRecord"/> to a compact, culture-invariant JSON string.
        /// </summary>
        /// <param name="record">The record to serialize.</param>
        /// <returns>A JSON string representation of <paramref name="record"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is null.</exception>
        public static string Serialize(ReviewRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var dto = new ReviewRecordDto
            {
                SchemaVersion = record.SchemaVersion,
                RuleId = record.RuleId,
                Reviewer = record.Reviewer,
                DateUtc = record.DateUtc.ToString("O", CultureInfo.InvariantCulture),
                MigrationFile = record.MigrationFile,
            };

            return JsonSerializer.Serialize(dto, Options);
        }

        /// <summary>
        /// Deserializes a JSON string produced by <see cref="Serialize(ReviewRecord)"/> back into a
        /// <see cref="ReviewRecord"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized <see cref="ReviewRecord"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON or is missing required fields.</exception>
        public static ReviewRecord Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(json));
            }

            var dto = JsonSerializer.Deserialize<ReviewRecordDto>(json, Options)
                ?? throw new JsonException("The JSON payload did not deserialize to a review record.");

            if (string.IsNullOrEmpty(dto.RuleId) || string.IsNullOrEmpty(dto.Reviewer) ||
                string.IsNullOrEmpty(dto.MigrationFile) || string.IsNullOrEmpty(dto.DateUtc))
            {
                throw new JsonException("The JSON payload is missing one or more required review record fields.");
            }

            var dateUtc = DateTime.Parse(
                dto.DateUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal);

            // schemaVersion defaults to 0 when absent, which identifies pre-versioning payloads;
            // treat those as schema version 1 (the original, unversioned shape).
            var schemaVersion = dto.SchemaVersion == 0 ? 1 : dto.SchemaVersion;

            return new ReviewRecord(dto.RuleId, dto.Reviewer, dateUtc, dto.MigrationFile, schemaVersion);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string produced by <see cref="Serialize(ReviewRecord)"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="record">Receives the deserialized <see cref="ReviewRecord"/> on success, or null on failure.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryDeserialize(string? json, out ReviewRecord? record)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                record = null;
                return false;
            }

            try
            {
                record = Deserialize(json!);
                return true;
            }
            catch (JsonException)
            {
                record = null;
                return false;
            }
            catch (ArgumentException)
            {
                record = null;
                return false;
            }
            catch (FormatException)
            {
                record = null;
                return false;
            }
        }

        /// <summary>
        /// The wire-format shape of a <see cref="ReviewRecord"/>. Kept separate from the public
        /// model so that JSON property names and casing stay stable even if the public model's
        /// property order or implementation changes.
        /// </summary>
        private sealed class ReviewRecordDto
        {
            /// <summary>The schema version the payload was written under.</summary>
            [JsonPropertyName("schemaVersion")]
            public int SchemaVersion { get; set; }

            /// <summary>The migration-safety diagnostic id.</summary>
            [JsonPropertyName("ruleId")]
            public string RuleId { get; set; } = string.Empty;

            /// <summary>The identity of the reviewer.</summary>
            [JsonPropertyName("reviewer")]
            public string Reviewer { get; set; } = string.Empty;

            /// <summary>The UTC review date/time, formatted with the round-trippable "O" specifier.</summary>
            [JsonPropertyName("dateUtc")]
            public string DateUtc { get; set; } = string.Empty;

            /// <summary>The path of the reviewed migration file.</summary>
            [JsonPropertyName("migrationFile")]
            public string MigrationFile { get; set; } = string.Empty;
        }
    }
}
