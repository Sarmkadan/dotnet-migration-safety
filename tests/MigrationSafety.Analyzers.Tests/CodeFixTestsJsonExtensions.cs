using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MigrationSafety.Analyzers.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="CodeFixTests"/>.
/// </summary>
public static class CodeFixTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static JsonSerializerOptions GetJsonOptions(bool indented)
    {
        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = indented,
        };
        return options;
    }

    /// <summary>
    /// Serializes the <see cref="CodeFixTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The code fix tests instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the code fix tests instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this CodeFixTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, GetJsonOptions(indented));
    }

    /// <summary>
    /// Deserializes a <see cref="CodeFixTests"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized code fix tests instance, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CodeFixTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<CodeFixTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="CodeFixTests"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized code fix tests instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out CodeFixTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<CodeFixTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}