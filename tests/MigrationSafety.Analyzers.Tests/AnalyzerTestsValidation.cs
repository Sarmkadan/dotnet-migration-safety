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
        /// Validates an <see cref="AnalyzerTests"/> instance and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The analyzer tests instance to validate.</param>
        /// <returns>A read-only list of validation problems; empty if the tests instance is valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this AnalyzerTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether an <see cref="AnalyzerTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The analyzer tests instance to check.</param>
        /// <returns><see langword="true"/> if the tests instance is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this AnalyzerTests value)
        {
            return value is not null;
        }

        /// <summary>
        /// Ensures that an <see cref="AnalyzerTests"/> instance is valid, throwing an exception if not.
        /// </summary>
        /// <param name="value">The analyzer tests instance to validate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The tests instance is not valid.</exception>
        public static void EnsureValid(this AnalyzerTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"AnalyzerTests instance is not valid. Problems:\n{string.Join("\n", problems)}");
            }
        }
    }
}