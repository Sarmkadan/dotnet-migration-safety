using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Provides System.Text.Json serialization and deserialization extensions for
    /// <see cref="MarkReviewedCodeFixProvider"/>.
    /// </summary>
    public static class MarkReviewedCodeFixProviderJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Serializes a <see cref="MarkReviewedCodeFixProvider"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MarkReviewedCodeFixProvider value, bool indented = false) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions)
        };

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MarkReviewedCodeFixProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized instance, or null if the JSON is invalid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        public static MarkReviewedCodeFixProvider? FromJson(string json) => string.IsNullOrWhiteSpace(json)
            ? throw new ArgumentException("Invalid JSON input", nameof(json))
            : JsonSerializer.Deserialize<MarkReviewedCodeFixProvider>(json, _jsonOptions);

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="MarkReviewedCodeFixProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized instance if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out MarkReviewedCodeFixProvider? value)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            try
            {
                value = JsonSerializer.Deserialize<MarkReviewedCodeFixProvider>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}