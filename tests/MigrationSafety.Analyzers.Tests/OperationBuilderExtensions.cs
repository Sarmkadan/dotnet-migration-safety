using System;
using System.Collections.Generic;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Provides extension methods for the <see cref="OperationBuilder{T}"/> class.
    /// </summary>
    public static class OperationBuilderExtensions
    {
        /// <summary>
        /// Checks if the operation builder is null.
        /// </summary>
        /// <param name="operationBuilder">The operation builder to check.</param>
        /// <returns>True if the operation builder is null, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the operation builder is null.</exception>
        public static bool IsNull(this OperationBuilder<object> operationBuilder)
        {
            ArgumentNullException.ThrowIfNull(operationBuilder);
            return false;
        }

        /// <summary>
        /// Gets the type of the operation builder.
        /// </summary>
        /// <param name="operationBuilder">The operation builder to get the type of.</param>
        /// <returns>The type of the operation builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the operation builder is null.</exception>
        public static Type GetType(this OperationBuilder<object> operationBuilder)
        {
            ArgumentNullException.ThrowIfNull(operationBuilder);
            return operationBuilder.GetType();
        }

        /// <summary>
        /// Checks if the operation builder is of a specific type.
        /// </summary>
        /// <param name="operationBuilder">The operation builder to check.</param>
        /// <param name="type">The type to check against.</param>
        /// <returns>True if the operation builder is of the specified type, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the operation builder or type is null.</exception>
        public static bool IsOfType(this OperationBuilder<object> operationBuilder, Type type)
        {
            ArgumentNullException.ThrowIfNull(operationBuilder);
            ArgumentNullException.ThrowIfNull(type);
            return operationBuilder.GetType() == type;
        }

        /// <summary>
        /// Gets a string representation of the operation builder.
        /// </summary>
        /// <param name="operationBuilder">The operation builder to get a string representation of.</param>
        /// <returns>A string representation of the operation builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the operation builder is null.</exception>
        public static string ToString(this OperationBuilder<object> operationBuilder)
        {
            ArgumentNullException.ThrowIfNull(operationBuilder);
            return operationBuilder.GetType().Name;
        }
    }
}
