using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrationSafety.Analyzers.Tests;

/// <summary>
/// Minimal stub type matching EF Core's OperationBuilder for JSON serialization support.
/// </summary>
/// <typeparam name="T">The operation type.</typeparam>
public class OperationBuilder<T>
{
}

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="OperationBuilder{T}"/>.
/// This class is sealed as it contains only static utility methods.
/// </summary>
public static class OperationBuilderJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Use camelCase for property names to match JavaScript/TypeScript conventions
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Compact JSON by default for better performance in tests
        WriteIndented = false,
        // Skip null values during serialization to reduce payload size
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes the <see cref="OperationBuilder{T}"/> to a JSON string using camelCase property naming and invariant culture.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="value">The operation builder to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the operation builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson<T>(this OperationBuilder<T> value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            { WriteIndented = true, }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an <see cref="OperationBuilder{T}"/> from a JSON string.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized operation builder, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">The JSON is invalid, empty, whitespace, or cannot be deserialized into an <see cref="OperationBuilder{T}"/>.</exception>
    public static OperationBuilder<T>? FromJson<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<OperationBuilder<T>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize an <see cref="OperationBuilder{T}"/> from a JSON string using camelCase property naming and invariant culture.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized operation builder if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson<T>(string json, out OperationBuilder<T>? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = default;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<OperationBuilder<T>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            // Deserialization failed due to invalid JSON
            return false;
        }
    }
}
