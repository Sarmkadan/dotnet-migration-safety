using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrationSafety.Analyzers.Tests
{
    public static class AnalyzerTestsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static JsonSerializerOptions GetJsonOptions(bool indented) =>
            new(_jsonOptions) { WriteIndented = indented };

        /// <summary>
        /// Serializes the <see cref="AnalyzerTests"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The analyzer tests instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the analyzer tests instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this AnalyzerTests value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            return JsonSerializer.Serialize(value, GetJsonOptions(indented));
        }

        /// <summary>
        /// Deserializes an <see cref="AnalyzerTests"/> instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized analyzer tests instance, or null if the JSON represents a null value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static AnalyzerTests? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<AnalyzerTests>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize an <see cref="AnalyzerTests"/> instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized analyzer tests instance if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out AnalyzerTests? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<AnalyzerTests>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
        }
    }
}
