# Migration Safety Analyzer

A Roslyn analyzer that flags destructive or lock-heavy EF Core migrations at build
time, before they reach production. It reads the `migrationBuilder.*` calls in your
generated migration files and warns on the operations that most often cause outages
and data loss.

## Rules

| Id | Operation | Why it hurts |
|----|-----------|--------------|
| [MIG001](docs/rules/MIG001.md) | `DropColumn` | Irreversible data loss; breaks older instances mid-deploy |
| [MIG002](docs/rules/MIG002.md) | `CreateIndex` | Plain `CREATE INDEX` locks writes for the whole build |
| [MIG003](docs/rules/MIG003.md) | `AlterColumn` (type / nullability change) | Full table rewrite under an exclusive lock |
| [MIG004](docs/rules/MIG004.md) | `DropTable` | Deletes every row; cannot be rolled forward |

All rules ship as **warnings** by default. Promote them to errors in CI with a line
in your `.editorconfig`:

```ini
dotnet_diagnostic.MIG001.severity = error
```

## Install

```sh
dotnet add package MigrationSafety.Analyzers
```

The package is a development dependency - it contributes no runtime assemblies to
your app, only the analyzer and its code fix.

## Suppressing a finding

The rules fire on generated code on purpose, so a blanket `#pragma` is the wrong
tool. When you have genuinely reviewed a risky operation, mark the statement:

```csharp
// migration-safety:reviewed Notes was never populated in any environment
migrationBuilder.DropColumn(name: "Notes", table: "Orders");
```

The analyzer honours that marker and stays quiet, and the justification stays in the
diff for the next reviewer. The bundled code fix ("Mark as reviewed") inserts the
marker for you with a `TODO` placeholder for the reason.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown, the
key design decisions (type-name matching, comment-based suppression, warning-first
severity) and how to add a new rule.

## Layout

```
src/
  MigrationSafety.Analyzers/           analyzer + diagnostics
  MigrationSafety.Analyzers.CodeFixes/ the "mark as reviewed" code fix
  MigrationSafety.Analyzers.Package/   NuGet packaging project
tests/
  MigrationSafety.Analyzers.Tests/     analyzer + code-fix tests
samples/
  OrdersMigration.cs                   a migration that trips every rule
docs/rules/                            one page per rule
```

## Building

```sh
dotnet build
dotnet test
dotnet pack src/MigrationSafety.Analyzers.Package -c Release
```

Requires the .NET SDK. The analyzer projects target `netstandard2.0` so they load
in any Roslyn host; the test project runs on the current .NET.

## Notes and limitations

- MIG002 flags every `CreateIndex` because the analyzer cannot know a table's row
  count at compile time. On genuinely small tables, use the reviewed marker.
- Detection matches EF Core's `MigrationBuilder` by type name, so a custom builder
  with the same name would also be inspected.
- Raw `migrationBuilder.Sql(...)` blocks are not parsed - the analyzer only sees the
  strongly typed builder API.

## AnalyzerTests

The `AnalyzerTests` class in `tests/MigrationSafety.Analyzers.Tests/AnalyzerTests.cs` contains integration tests that verify the MigrationSafety analyzers correctly identify unsafe EF Core migration operations. Each test method validates that a specific migration operation triggers (or does not trigger) the expected diagnostic when the analyzer processes generated migration code.

The class provides a reusable pattern for testing analyzer behavior:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        // Test various migration operations
        migrationBuilder.DropColumn(name: "Notes", table: "Orders");
        migrationBuilder.DropTable(name: "LegacyAudit");
        migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId", table: "Orders", column: "CustomerId");
        migrationBuilder.AlterColumn<string>(name: "Status", table: "Orders");
        migrationBuilder.AddColumn<string>(name: "Notes", table: "Orders");
        
        // Positional arguments are also supported
        migrationBuilder.DropColumn("Notes", "Orders");
        
        // Suppression marker works correctly
        // migration-safety:reviewed column is empty in every environment
        migrationBuilder.DropColumn("Notes", "Orders");
    }
}
```

The test suite includes methods like `DropColumn_is_flagged`, `DropTable_is_flagged`, `CreateIndex_is_flagged`, `AlterColumn_is_flagged`, `Positional_arguments_are_understood`, `AddColumn_is_not_flagged`, `Reviewed_marker_suppresses_the_diagnostic`, and `Unrelated_type_named_similarly_is_ignored` to validate all analyzer rules.

## OperationBuilder
The `OperationBuilder<T>` class is used to construct database operations such such as adding columns, creating indexes, altering columns, dropping columns, and dropping tables. It provides a fluent API for building migration operations with type safety.

Example usage:
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public class MyMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add a new column to the Orders table
        migrationBuilder.OperationBuilder()
            .AddColumn<string>(name: "Notes", table: "Orders")
            .Nullable()
            .ColumnType("nvarchar(max)");

        // Create an index on the CustomerId column
        migrationBuilder.OperationBuilder()
            .CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId"
            )
            .IsUnique();

        // Alter an existing column
        migrationBuilder.OperationBuilder()
            .AlterColumn<string>(name: "Status", table: "Orders")
            .IsRequired();

        // Drop a column
        migrationBuilder.DropColumn(name: "LegacyField", table: "Orders");

        // Drop a table
        migrationBuilder.DropTable(name: "OldAuditLog");

        // Execute raw SQL
        migrationBuilder.OperationBuilder().Sql("UPDATE Orders SET Status = 'Active' WHERE Status IS NULL");
    }
}
```

## MarkReviewedCodeFixProviderTests

The `MarkReviewedCodeFixProviderTests` class contains unit tests verifying the correctness of the `MarkReviewedCodeFixProvider`. It ensures the provider correctly identifies fixable diagnostic IDs, supports the required `FixAllProvider`, and adheres to expected behavioral constraints, such as ensuring diagnostic ID sets are immutable and unique.

```csharp
using MigrationSafety.Analyzers;
using MigrationSafety.Analyzers.Tests;
using Xunit;

public class MarkReviewedCodeFixProviderDemo
{
    public void Run()
    {
        var tests = new MarkReviewedCodeFixProviderTests();

        // Validate the provider functionality
        tests.FixableDiagnosticIds_ReturnsExpectedDiagnostics();
        tests.GetFixAllProvider_ReturnsValidProvider();
        tests.FixableDiagnosticIds_IsImmutable();
        tests.FixableDiagnosticIds_ReturnsSameDiagnosticIds();
        tests.ProviderName_MatchesExpected();
        tests.Provider_IsShared();
        tests.FixableDiagnosticIds_DoesNotContainNull();
        tests.FixableDiagnosticIds_ContainsUniqueValues();
    }
}
```

## MarkReviewedCodeFixProviderJsonExtensionsTests

The `MarkReviewedCodeFixProviderJsonExtensionsTests` class contains unit tests for JSON serialization and deserialization of `MarkReviewedCodeFixProvider` instances. It verifies the correctness of `ToJson` and `FromJson` methods, including happy path scenarios and error handling for null or empty inputs.

Here's an example of how to use the `ToJson` and `FromJson` methods:
```csharp
var provider = new MarkReviewedCodeFixProvider();
var json = provider.ToJson();
var deserializedProvider = MarkReviewedCodeFixProviderJsonExtensions.FromJson(json);
```

## MigrationBuilderCallsTests

The `MigrationBuilderCallsTests` class contains unit tests that verify `MigrationBuilderCalls.IsMigrationBuilderMethod` correctly identifies EF Core `MigrationBuilder` methods while ignoring identically named methods on unrelated types. These tests utilize Roslyn to compile small code snippets and inspect the resulting semantic model to ensure accurate detection.

Here is an example showing how to invoke these test methods:

```csharp
public class MigrationBuilderCallsTestsDemo
{
    public void Test()
    {
        var tests = new MigrationBuilderCallsTests();

        // Verify that MigrationBuilder methods are correctly recognized
        tests.MigrationBuilderMethod_is_recognized();

        // Verify that identically-named methods on unrelated types are not recognized
        tests.UnrelatedMethod_is_not_recognized();
    }
}
```

## License


MIT. See [LICENSE](LICENSE).

## SuppressionCommentTests

The `SuppressionCommentTests` class contains unit tests that verify the behavior of the `SuppressionComment` helper, ensuring it correctly detects review markers in various comment styles and placements within generated migration code. Each test method exercises a different scenario, such as comments on the line above, uppercase markers, mixed‑case markers, and multi‑line comments.

```csharp
using MigrationSafety.Analyzers;
using MigrationSafety.Analyzers.Tests;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class SuppressionCommentDemo
{
    public void Run()
    {
        var tests = new SuppressionCommentTests();

        // Run a specific test scenario
        tests.IsReviewed_WithCommentOnLineAbove_ReturnsTrue();

        // Obtain a statement syntax node from the test helper
        StatementSyntax stmt = tests.Up();

        // Check whether the statement is considered reviewed
        bool isReviewed = SuppressionComment.IsReviewed(stmt);
    }
}
```

The demo shows how the public members of `SuppressionCommentTests` can be used to drive the same logic that the test suite validates, providing a clear example of the API in action. 
