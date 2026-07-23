using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Unit tests for SuppressionComment.IsReviewed method.
    /// Tests various scenarios for detecting the migration-safety:reviewed marker comment.
    /// </summary>
    public class SuppressionCommentTests
    {
        /// <summary>
        /// Verifies that IsReviewed returns true when the marker comment is on the line above the invocation.
        /// This is the primary use case for suppression comments.
        /// </summary>
        [Fact]
        public void IsReviewed_WithCommentOnLineAbove_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed safe to drop this column
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker uses different casing (uppercase).
        /// The method uses case-insensitive comparison.
        /// </summary>
        [Fact]
        public void IsReviewed_WithUppercaseMarker_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // MIGRATION-SAFETY:REVIEWED safe to drop
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker uses different casing (mixed case).
        /// The method uses case-insensitive comparison.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMixedCaseMarker_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // Migration-Safety:Reviewed safe to drop
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns false when there is no marker comment.
        /// This ensures the method correctly identifies when a statement is not reviewed.
        /// </summary>
        [Fact]
        public void IsReviewed_WithoutMarker_ReturnsFalse()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns false when the marker text is incorrect.
        /// The comment must contain the exact marker text.
        /// </summary>
        [Fact]
        public void IsReviewed_WithWrongMarkerText_ReturnsFalse()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:approved safe to drop this column
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker is on the line immediately above the invocation,
        /// even if there are other comments in between.
        /// In C#, comments on the line immediately before a statement are part of the statement's leading trivia.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMarkerOnLineAbove_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed safe to drop this column
        // This is an additional comment line
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when using multi-line comment syntax.
        /// The method should handle both single-line and multi-line comments.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMultiLineComment_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        /* migration-safety:reviewed safe to drop this column */
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker is followed by additional text.
        /// The marker text can be part of a larger comment.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMarkerAndAdditionalText_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed (MIG001) TODO: document why this is safe
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns false when the marker appears in a trailing comment on the same line.
        /// The method only checks leading trivia, not trailing trivia.
        /// </summary>
        [Fact]
        public void IsReviewed_WithTrailingCommentOnSameLine_ReturnsFalse()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders""); // migration-safety:reviewed
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that IsReviewed works correctly with different migration operations.
        /// </summary>
        [Fact]
        public void IsReviewed_WorksWithDifferentOperations()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed safe operation
        migrationBuilder.Sql(""DROP TABLE Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker has extra whitespace around the colon.
        /// The parser should tolerate formatting drift from dotnet format or manual editing.
        /// </summary>
        [Fact]
        public void IsReviewed_WithWhitespaceAroundColon_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety : reviewed safe to drop
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker has extra whitespace throughout.
        /// Tests tolerance for multiple spaces, tabs, etc.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMultipleWhitespaceVariations_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        //    migration-safety   :   reviewed   (MIG001)   safe to drop column
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker uses parentheses with diagnostic ID.
        /// This is the format that the code fix provider generates.
        /// </summary>
        [Fact]
        public void IsReviewed_WithDiagnosticIdInParentheses_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed (MIG001) TODO: document why this is safe
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker has lowercase letters.
        /// Tests case-insensitive matching.
        /// </summary>
        [Fact]
        public void IsReviewed_WithLowercaseMarker_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed safe to drop
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when using multi-line comment with variations.
        /// Tests that multi-line comments also support formatting tolerance.
        /// </summary>
        [Fact]
        public void IsReviewed_WithMultiLineCommentVariations_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        /* MIGRATION-SAFETY : REVIEWED (mig001) */
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker has trailing reviewer information.
        /// Tests that additional text after the marker is accepted.
        /// </summary>
        [Fact]
        public void IsReviewed_WithTrailingReviewerInfo_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed reviewed by Alice on 2024-01-15
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsReviewed returns true when the marker has extra text in parentheses.
        /// Tests various formats that might be used by developers.
        /// </summary>
        [Fact]
        public void IsReviewed_WithExtraParenthesesContent_ReturnsTrue()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // migration-safety:reviewed (MIG001:DropColumn) safe operation
        migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders"");
    }
}
";

            var result = TestSuppressionComment(source);
            Assert.True(result);
        }


        /// <summary>
        /// Helper method to test SuppressionComment.IsReviewed with a given source code.
        /// </summary>
        /// <param name="source">The source code containing the migration</param>
        /// <returns>True if the suppression comment is detected, false otherwise</returns>
        private static bool TestSuppressionComment(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(MigrationBuilderStub.Source + source);
            var compilation = CSharpCompilation.Create("TestAssembly")
                .WithReferences(new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.Location),
                })
                .AddSyntaxTrees(tree);

            var root = tree.GetRoot();

            // Find the invocation expression for the method
            var invocation = root.DescendantNodes()
                .Where(n => n is InvocationExpressionSyntax)
                .Cast<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocation == null)
            {
                throw new InvalidOperationException("Could not find invocation");
            }

            return SuppressionComment.IsReviewed(invocation);
        }
    }
}