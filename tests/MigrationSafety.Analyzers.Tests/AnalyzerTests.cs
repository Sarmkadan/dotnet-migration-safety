using System.Threading.Tasks;
using Xunit;


/// <summary>
/// Contains integration tests for the MigrationSafety analyzers that verify the diagnostic rules
/// are correctly identifying unsafe migration operations in Entity Framework Core migrations.
/// </summary>
namespace MigrationSafety.Analyzers.Tests
{
    public class AnalyzerTests
    {

/// <summary>
/// Template header for migration test code that wraps the test body.
/// </summary>
        private const string Header = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {

/// <summary>
/// Template footer for migration test code that completes the class definition.
/// </summary>
";
        private const string Footer = @"
    }

/// <summary>
/// Wraps the provided migration body code with the necessary header and footer to create
/// a complete compilable migration class for testing purposes.
/// </summary>
/// <param name="body">The migration operation code to be tested.</param>
/// <returns>A complete migration class as a string with the body inserted.</returns>
}

	/// <summary>
	/// Verifies that the analyzer correctly flags <c>DropColumn</c> operations as unsafe migrations.
	/// The MIG001 diagnostic should be triggered for this operation.
	/// </summary>
";

        private static string Wrap(string body) => Header + body + Footer;

        [Fact]
        public async Task DropColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(

	/// <summary>
	/// Verifies that the analyzer correctly flags <c>DropTable</c> operations as unsafe migrations.
	/// The MIG004 diagnostic should be triggered for this operation.
	/// </summary>
                @"migrationBuilder.{|MIG001:DropColumn|}(name: ""Notes"", table: ""Orders"");"));
        }

        [Fact]
        public async Task DropTable_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG004:DropTable|}(name: ""LegacyAudit"");"));

	/// <summary>
	/// Verifies that the analyzer correctly flags <c>CreateIndex</c> operations as unsafe migrations.
	/// The MIG002 diagnostic should be triggered for this operation.
	/// </summary>
        }

        [Fact]
        public async Task CreateIndex_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG002:CreateIndex|}(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId"");"));
        }

	/// <summary>
	/// Verifies that the analyzer correctly flags <c>AlterColumn</c> operations as unsafe migrations.
	/// The MIG003 diagnostic should be triggered for this operation.
	/// </summary>

        [Fact]
        public async Task AlterColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG003:AlterColumn<string>|}(name: ""Status"", table: ""Orders"");"));
        }


	/// <summary>
	/// Verifies that the analyzer correctly handles positional arguments in migration operations.
	/// Ensures that MIG001 diagnostic is triggered even when using positional instead of named arguments.
	/// </summary>
        [Fact]
        public async Task Positional_arguments_are_understood()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG001:DropColumn|}(""Notes"", ""Orders"");"));
        }

        [Fact]

	/// <summary>
	/// Verifies that the analyzer does not flag <c>AddColumn</c> operations as they are considered safe migrations.
	/// No diagnostic should be triggered for this operation.
	/// </summary>
        public async Task AddColumn_is_not_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.AddColumn<string>(""Notes"", ""Orders"");"));
        }

        [Fact]
        public async Task Reviewed_marker_suppresses_the_diagnostic()

	/// <summary>
	/// Verifies that the <c>// migration-safety:reviewed</c> marker suppresses the diagnostic for unsafe operations.
	/// The MIG001 diagnostic should not be triggered when the reviewed marker is present.
	/// </summary>
        {
            await Verify.AnalyzerAsync(Wrap(
                @"// migration-safety:reviewed column is empty in every environment
        migrationBuilder.DropColumn(""Notes"", ""Orders"");"));
        }

        [Fact]
        public async Task Unrelated_type_named_similarly_is_ignored()

	/// <summary>
	/// Verifies that the analyzer correctly ignores unrelated types with similar names.
	/// Ensures that only actual MigrationBuilder calls are analyzed, not arbitrary methods named DropColumn.
	/// </summary>
        {
            await Verify.AnalyzerAsync(@"
public class NotABuilder
{
    public void DropColumn(string name, string table) { }
}

public class Caller
{
    public void Go()
    {
        new NotABuilder().DropColumn(""Notes"", ""Orders"");
    }
}
");
        }
    }
}
