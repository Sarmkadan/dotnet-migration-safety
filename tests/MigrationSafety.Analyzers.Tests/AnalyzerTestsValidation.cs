using System;
using System.Collections.Generic;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Provides validation helpers for analyzer test classes.
    /// </summary>
    public static class AnalyzerTestsValidation
    {
        /// <summary>
        /// Validates that the analyzer tests instance is not null.
        /// </summary>
        /// <param name="value">The analyzer tests instance to validate.</param>
        /// <returns>A list of validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this AnalyzerTests value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether the analyzer tests instance is valid.
        /// </summary>
        /// <param name="value">The analyzer tests instance to check.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public static bool IsValid(this AnalyzerTests value)
        {
            return value != null;
        }

        /// <summary>
        /// Ensures that the analyzer tests instance is valid, throwing an exception if not.
        /// </summary>
        /// <param name="value">The analyzer tests instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this AnalyzerTests value)
        {
            ArgumentNullException.ThrowIfNull(value);
        }
    }
}
