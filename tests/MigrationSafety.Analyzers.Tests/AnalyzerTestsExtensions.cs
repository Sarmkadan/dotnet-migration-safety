using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="AnalyzerTests"/> instances in
    /// ad‑hoc scenarios (e.g., debugging or custom test runners).
    /// </summary>
    public static class AnalyzerTestsExtensions
    {
        /// <summary>
        /// Returns the names of all public instance methods on <see cref="AnalyzerTests"/> that return a <see cref="Task"/>.
        /// </summary>
        /// <param name="tests">The <see cref="AnalyzerTests"/> instance.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the method names.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> GetAsyncTestNames(this AnalyzerTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return tests.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(static m => typeof(Task).IsAssignableFrom(m.ReturnType))
                .Select(static m => m.Name)
                .ToArray();
        }

        /// <summary>
        /// Executes all public async test methods on the supplied <see cref="AnalyzerTests"/> instance
        /// sequentially. The method completes when every test has finished or when the first exception
        /// propagates.
        /// </summary>
        /// <param name="tests">The <see cref="AnalyzerTests"/> instance.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static async Task RunAllAsync(this AnalyzerTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            foreach (var method in tests.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(static m => typeof(Task).IsAssignableFrom(m.ReturnType))
                .Cast<MethodInfo>())
            {
                var result = (Task)method.Invoke(tests, null)!;
                await result.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to run a single async test method identified by <paramref name="testName"/>.
        /// Returns <c>true</c> if the method was found and completed without throwing; otherwise
        /// returns <c>false</c>.
        /// </summary>
        /// <param name="tests">The <see cref="AnalyzerTests"/> instance.</param>
        /// <param name="testName">The exact name of the test method to invoke.</param>
        /// <returns>A task that resolves to <c>true</c> on success, <c>false</c> on failure.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tests"/> or <paramref name="testName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="testName"/> is an empty string.</exception>
        public static async Task<bool> TryRunTestAsync(this AnalyzerTests tests, string testName)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrEmpty(testName);

            var method = tests.GetType()
                .GetMethod(testName, BindingFlags.Instance | BindingFlags.Public);

            if (method is null || !typeof(Task).IsAssignableFrom(method.ReturnType))
                return false;

            try
            {
                var task = (Task)method.Invoke(tests, null)!;
                await task.ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}