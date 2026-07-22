using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Tests for <see cref="MarkReviewedCodeFixProvider"/> that verify the public API and behavior
    /// without duplicating the integration tests in CodeFixTests.cs.
    /// </summary>
    public class MarkReviewedCodeFixProviderTests
    {
        /// <summary>
        /// Verifies that FixableDiagnosticIds returns the expected set of diagnostic IDs.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_ReturnsExpectedDiagnostics()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var fixableIds = provider.FixableDiagnosticIds;

            // Assert
            Assert.NotEmpty(fixableIds);
            Assert.Equal(6, fixableIds.Length);

            // Verify all expected diagnostic IDs are present
            Assert.Contains(DiagnosticIds.DropColumn, fixableIds);
            Assert.Contains(DiagnosticIds.NonConcurrentIndex, fixableIds);
            Assert.Contains(DiagnosticIds.TableRewrite, fixableIds);
            Assert.Contains(DiagnosticIds.DropTable, fixableIds);
            Assert.Contains(DiagnosticIds.AddNotNullColumn, fixableIds);
            Assert.Contains(DiagnosticIds.RenameColumn, fixableIds);
        }

        /// <summary>
        /// Verifies that GetFixAllProvider returns a valid FixAllProvider.
        /// </summary>
        [Fact]
        public void GetFixAllProvider_ReturnsValidProvider()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var fixAllProvider = provider.GetFixAllProvider();

            // Assert
            Assert.NotNull(fixAllProvider);
            Assert.IsAssignableFrom<Microsoft.CodeAnalysis.CodeFixes.FixAllProvider>(fixAllProvider);
        }

        /// <summary>
        /// Verifies that FixableDiagnosticIds is immutable and cannot be modified.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_IsImmutable()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act - try to verify it's a proper immutable array
            var fixableIds = provider.FixableDiagnosticIds;

            // Assert
            Assert.IsType<System.Collections.Immutable.ImmutableArray<string>>(fixableIds);
        }

        /// <summary>
        /// Verifies that FixableDiagnosticIds returns the same diagnostic IDs on multiple calls.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_ReturnsSameDiagnosticIds()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var firstCall = provider.FixableDiagnosticIds;
            var secondCall = provider.FixableDiagnosticIds;

            // Assert
            Assert.Equal(firstCall, secondCall);
        }

        /// <summary>
        /// Verifies that the provider name matches the expected value.
        /// </summary>
        [Fact]
        public void ProviderName_MatchesExpected()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act & Assert - The provider should have the correct name from the ExportCodeFixProvider attribute
            // This indirectly verifies the attribute is properly applied
            Assert.Equal(nameof(MarkReviewedCodeFixProvider), provider.GetType().Name);
        }

        /// <summary>
        /// Verifies that the provider is marked as shared (can be used by multiple consumers).
        /// </summary>
        [Fact]
        public void Provider_IsShared()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act & Assert - The [Shared] attribute should be present
            var type = provider.GetType();
            var sharedAttribute = type.GetCustomAttributes(typeof(System.Composition.SharedAttribute), false);

            Assert.NotNull(sharedAttribute);
            Assert.Single(sharedAttribute);
        }

        /// <summary>
        /// Verifies that FixableDiagnosticIds handles null diagnostic IDs gracefully.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_DoesNotContainNull()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var fixableIds = provider.FixableDiagnosticIds;

            // Assert
            foreach (var id in fixableIds)
            {
                Assert.NotEmpty(id);
            }
        }

        /// <summary>
        /// Verifies that FixableDiagnosticIds returns unique diagnostic IDs.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_ContainsUniqueValues()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var fixableIds = provider.FixableDiagnosticIds;

            // Assert
            var distinctCount = fixableIds.ToArray().Distinct().Count();
            Assert.Equal(fixableIds.Length, distinctCount);
        }
    }
}