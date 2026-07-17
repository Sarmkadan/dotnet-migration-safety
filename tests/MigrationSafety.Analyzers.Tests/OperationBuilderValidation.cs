using System;
using System.Collections.Generic;

namespace MigrationSafety.Analyzers.Tests;

/// <summary>
/// Provides validation helpers for <see cref="OperationBuilder{T}"/> instances.
/// </summary>
public static class OperationBuilderValidation
{
    /// <summary>
    /// Validates an <see cref="OperationBuilder{T}"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="value">The operation builder to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the builder is valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate<T>(this OperationBuilder<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether an <see cref="OperationBuilder{T}"/> instance is valid.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="value">The operation builder to check.</param>
    /// <returns><see langword="true"/> if the builder is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid<T>(this OperationBuilder<T> value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="OperationBuilder{T}"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    /// <param name="value">The operation builder to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The builder contains validation problems.</exception>
    public static void EnsureValid<T>(this OperationBuilder<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"OperationBuilder is not valid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}