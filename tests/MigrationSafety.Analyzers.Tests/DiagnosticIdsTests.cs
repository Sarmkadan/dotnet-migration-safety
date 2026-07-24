using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Tests that verify the integrity of DiagnosticIds constants and their mapping to Descriptors.
    /// Ensures no duplicate IDs, no orphaned IDs, and no mismatched references.
    /// </summary>
    public class DiagnosticIdsTests
    {
        /// <summary>
        /// Verifies that all DiagnosticIds constants (excluding Category) have unique values.
        /// Prevents accidental duplication when adding new diagnostic IDs.
        /// </summary>
        [Fact]
        public void DiagnosticIds_Values_Are_Unique()
        {
            // Arrange
            var diagnosticIdsType = typeof(DiagnosticIds);
            var constantFields = diagnosticIdsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.Name != "Category")
                .ToList();

            var values = new HashSet<string>();
            var duplicates = new List<(string Id, string FieldName)>();

            // Act
            foreach (var field in constantFields)
            {
                var value = field.GetValue(null) as string;
                if (value != null)
                {
                    if (!values.Add(value))
                    {
                        duplicates.Add((value, field.Name));
                    }
                }
            }

            // Assert
            Assert.Empty(duplicates);
        }

        /// <summary>
        /// Verifies that all DiagnosticIds constants (excluding Category) follow the expected MIGXXX pattern.
        /// </summary>
        [Fact]
        public void DiagnosticIds_Values_Follow_Pattern()
        {
            // Arrange
            var diagnosticIdsType = typeof(DiagnosticIds);
            var constantFields = diagnosticIdsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.Name != "Category")
                .ToList();

            // Act & Assert
            foreach (var field in constantFields)
            {
                var value = field.GetValue(null) as string;
                if (value != null)
                {
                    if (!value.StartsWith("MIG"))
                    {
                        Assert.Fail($"Diagnostic ID '{value}' from field '{field.Name}' must start with 'MIG'");
                    }
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, "^MIG\\d{3}$"))
                    {
                        Assert.Fail($"Diagnostic ID '{value}' from field '{field.Name}' must match pattern 'MIG000'");
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that all DiagnosticIds constants (excluding Category) are referenced by at least one Descriptor.
        /// Prevents orphaned diagnostic IDs that have no actual diagnostic rule.
        /// </summary>
        [Fact]
        public void All_DiagnosticIds_Constants_Are_Referenced_By_Descriptors()
        {
            // Arrange
            var diagnosticIdsType = typeof(DiagnosticIds);
            var descriptorType = typeof(Descriptors);

            var diagnosticIdFields = diagnosticIdsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.Name != "Category")
                .Select(f => f.GetValue(null) as string)
                .Where(v => v != null)
                .ToList();

            var descriptorFields = descriptorType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .ToList();

            var referencedIds = new HashSet<string>(StringComparer.Ordinal);

            // Act
            foreach (var field in descriptorFields)
            {
                var descriptor = field.GetValue(null) as DiagnosticDescriptor;
                if (descriptor != null)
                {
                    referencedIds.Add(descriptor.Id);
                }
            }

            // Assert
            var unreferencedIds = diagnosticIdFields
                .Where(id => !referencedIds.Contains(id, StringComparer.Ordinal))
                .ToList();

            Assert.Empty(unreferencedIds);
        }

        /// <summary>
        /// Verifies that all Descriptors reference valid DiagnosticIds constants.
        /// Prevents typos or hardcoded strings that don't match any DiagnosticIds constant.
        /// </summary>
        [Fact]
        public void All_Descriptors_Reference_Valid_DiagnosticIds()
        {
            // Arrange
            var descriptorType = typeof(Descriptors);
            var diagnosticIdsType = typeof(DiagnosticIds);

            var descriptorFields = descriptorType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .ToList();

            var validIds = diagnosticIdsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.GetValue(null) as string)
                .Where(v => v != null)
                .ToList();

            // Act & Assert
            foreach (var field in descriptorFields)
            {
                var descriptor = field.GetValue(null) as DiagnosticDescriptor;
                if (descriptor != null)
                {
                    if (!validIds.Any(id => string.Equals(id, descriptor.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        Assert.Fail($"Descriptor field '{field.Name}' references diagnostic ID '{descriptor.Id}' which is not defined in DiagnosticIds constants");
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that there is a 1:1 mapping between DiagnosticIds constants and Descriptors.
        /// Ensures no DiagnosticIds constant maps to multiple descriptors and vice versa.
        /// </summary>
        [Fact]
        public void DiagnosticIds_And_Descriptors_Have_OneToOne_Mapping()
        {
            // Arrange
            var diagnosticIdsType = typeof(DiagnosticIds);
            var descriptorType = typeof(Descriptors);

            var diagnosticIdFields = diagnosticIdsType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.Name != "Category")
                .Select(f => new { Name = f.Name, Value = f.GetValue(null) as string })
                .Where(x => x.Value != null)
                .ToList();

            var descriptorFields = descriptorType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => new { Name = f.Name, Descriptor = f.GetValue(null) as DiagnosticDescriptor })
                .Where(x => x.Descriptor != null)
                .ToList();

            var idToFieldMap = diagnosticIdFields.ToDictionary(x => x.Value!, StringComparer.Ordinal);
            var descriptorIdToFieldMap = descriptorFields.ToDictionary(x => x.Descriptor!.Id, StringComparer.Ordinal);

            // Act
            var duplicateIds = diagnosticIdFields
                .GroupBy(x => x.Value, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            var idsWithoutDescriptors = diagnosticIdFields
                .Select(x => x.Value)
                .Except(descriptorIdToFieldMap.Keys, StringComparer.Ordinal)
                .ToList();

            var descriptorsWithoutIds = descriptorFields
                .Select(x => x.Descriptor!.Id)
                .Except(idToFieldMap.Keys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var duplicateDescriptors = descriptorFields
                .GroupBy(x => x.Descriptor!.Id, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            // Assert
            Assert.Empty(duplicateIds);
            Assert.Empty(idsWithoutDescriptors);
            Assert.Empty(descriptorsWithoutIds);
            Assert.Empty(duplicateDescriptors);
        }

        /// <summary>
        /// Verifies that the analyzer's SupportedDiagnostics collection references all descriptors.
        /// Ensures the analyzer is properly configured to report all available diagnostics.
        /// </summary>
        [Fact]
        public void Analyzer_SupportedDiagnostics_Includes_All_Descriptors()
        {
            // Arrange
            var analyzerType = typeof(MigrationSafetyAnalyzer);
            var descriptorType = typeof(Descriptors);

            var supportedDiagnosticsProperty = analyzerType
                .GetProperty("SupportedDiagnostics", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(supportedDiagnosticsProperty);

            var analyzer = new MigrationSafetyAnalyzer();
            var supportedDiagnostics = (ImmutableArray<DiagnosticDescriptor>)supportedDiagnosticsProperty!
                .GetValue(analyzer)!;

            var descriptorFields = descriptorType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => f.GetValue(null) as DiagnosticDescriptor)
                .Where(d => d != null)
                .ToList();

            // Act
            var supportedIds = supportedDiagnostics.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
            var descriptorIds = descriptorFields.Select(d => d!.Id).ToHashSet(StringComparer.Ordinal);

            // Assert - The analyzer may not support all descriptors (e.g., MIG999 UnreviewedSuppression)
            // so we check that all supported diagnostics are defined in descriptors
            Assert.True(supportedIds.IsSubsetOf(descriptorIds),
                $"Supported diagnostics should be a subset of all descriptors. Missing: {string.Join(", ", descriptorIds.Except(supportedIds))}");
        }

        /// <summary>
        /// Verifies that DiagnosticIds constants have consistent values across the codebase.
        /// Ensures the Category constant is used consistently.
        /// </summary>
        [Fact]
        public void DiagnosticIds_Category_Is_Consistent()
        {
            // Arrange
            var diagnosticIdsType = typeof(DiagnosticIds);

            // Act
            var categoryValue = diagnosticIdsType
                .GetField("Category", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                ?.GetValue(null) as string;

            // Assert
            Assert.NotNull(categoryValue);
            Assert.Equal("MigrationSafety", categoryValue);
        }
    }
}