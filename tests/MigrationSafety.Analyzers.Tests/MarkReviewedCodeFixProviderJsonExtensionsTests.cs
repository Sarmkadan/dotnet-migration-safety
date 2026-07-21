using System;
using System.Text.Json;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    public class MarkReviewedCodeFixProviderJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();

            // Act
            var json = MarkReviewedCodeFixProviderJsonExtensions.ToJson(provider);

            // Assert
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => MarkReviewedCodeFixProviderJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsProvider()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();
            var json = MarkReviewedCodeFixProviderJsonExtensions.ToJson(provider);

            // Act
            var result = MarkReviewedCodeFixProviderJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(provider, result);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => MarkReviewedCodeFixProviderJsonExtensions.FromJson(null));
        }

        [Fact]
        public void FromJson_EmptyString_ReturnsNull()
        {
            // Act
            var result = MarkReviewedCodeFixProviderJsonExtensions.FromJson("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue()
        {
            // Arrange
            var provider = new MarkReviewedCodeFixProvider();
            var json = MarkReviewedCodeFixProviderJsonExtensions.ToJson(provider);

            // Act
            var result = MarkReviewedCodeFixProviderJsonExtensions.TryFromJson(json, out var value);

            // Assert
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal(provider, value);
        }

        [Fact]
        public void TryFromJson_NullInput_ReturnsFalse()
        {
            // Act
            var result = MarkReviewedCodeFixProviderJsonExtensions.TryFromJson(null, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryFromJson_EmptyString_ReturnsFalse()
        {
            // Act
            var result = MarkReviewedCodeFixProviderJsonExtensions.TryFromJson("", out _);

            // Assert
            Assert.False(result);
        }
    }
}
