using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Contains integration tests for the MigrationSafety analyzers that verify the diagnostic rules are correctly identifying unsafe migration operations in Entity Framework Core migrations.
/// </summary>
namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Integration tests for the MigrationSafety analyzers. Each test verifies that a specific migration operation
    /// triggers (or does not trigger) the expected diagnostic when the analyzer processes the generated migration code.
    /// </summary>
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
        /// <param name=""body"">The migration operation code to be tested.</param>
        /// <returns>A complete migration class as a string with the body inserted.</returns>
        ";

        private static string Wrap(string body) => Header + body + Footer;

        /// <summary>
        /// Verifies that the analyzer flags a <c>DropColumn</c> operation as unsafe, emitting the MIG001 diagnostic.
        /// </summary>
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

        /// <summary>
        /// Verifies that the analyzer flags a <c>DropTable</c> operation as unsafe, emitting the MIG004 diagnostic.
        /// </summary>
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

        /// <summary>
        /// Verifies that the analyzer flags a <c>CreateIndex</c> operation as unsafe, emitting the MIG002 diagnostic.
        /// </summary>
        [Fact]
        public async Task CreateIndex_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG002:CreateIndex|}(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId"");"));
        }

        /// <summary>
        /// Verifies that the analyzer flags an <c>AlterColumn</c> operation as unsafe, emitting the MIG003 diagnostic.
        /// </summary>
        [Fact]
        public async Task AlterColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG003:AlterColumn<string>|}(name: ""Status"", table: ""Orders"");"));
        }

        /// <summary>
        /// Verifies that the analyzer correctly understands positional arguments and still emits the MIG001 diagnostic for a <c>DropColumn</c> call.
        /// </summary>
        [Fact]
        public async Task Positional_arguments_are_understood()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG001:DropColumn|}(""Notes"", ""Orders"");"));
        }

        /// <summary>
        /// Verifies that the analyzer does not flag an <c>AddColumn</c> operation, as it is considered a safe migration (no diagnostic expected).
        /// </summary>
        [Fact]
        public async Task AddColumn_is_not_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.AddColumn<string>(""Notes"", ""Orders"");"));
        }

        /// <summary>
        /// Verifies that the <c>// migration-safety:reviewed</c> marker suppresses the MIG001 diagnostic for an unsafe operation.
        /// </summary>
        [Fact]
        public async Task Reviewed_marker_suppresses_the_diagnostic()
        {
            /// <summary>
            /// Verifies that the <c>// migration-safety:reviewed</c> marker suppresses the diagnostic for unsafe operations.
            /// The MIG001 diagnostic should not be triggered when the reviewed marker is present.
            /// </summary>
            await Verify.AnalyzerAsync(Wrap(
                @"// migration-safety:reviewed column is empty in every environment
        migrationBuilder.DropColumn(""Notes"", ""Orders"");"));
        }

        /// <summary>
        /// Verifies that the analyzer ignores methods named like migration operations when they are defined on unrelated types.
        /// No diagnostic should be produced for such calls.
        /// </summary>
        [Fact]
        public async Task Unrelated_type_named_similarly_is_ignored()
        {
            /// <summary>
            /// Verifies that the analyzer correctly ignores unrelated types with similar names.
            /// Ensures that only actual MigrationBuilder calls are analyzed, not arbitrary methods named DropColumn.
            /// </summary>
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

        /// <summary>
        /// Verifies that the analyzer flags a <c>RenameColumn</c> operation as unsafe, emitting the MIG006 diagnostic.
        /// </summary>
        [Fact]
        public async Task RenameColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG006:RenameColumn|}(name: ""OldCol"", table: ""MyTable"", newName: ""NewCol"");"));
        }

        /// <summary>
        /// Verifies that the analyzer flags a <c>RenameTable</c> operation as unsafe, emitting the MIG006 diagnostic.
        /// </summary>
        [Fact]
        public async Task RenameTable_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG006:RenameTable|}(name: ""OldTable"", newName: ""NewTable"");"));
        }
    }
}
