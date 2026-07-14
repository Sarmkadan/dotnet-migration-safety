using System.Threading.Tasks;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    public class AnalyzerTests
    {
        private const string Header = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
";
        private const string Footer = @"
    }
}
";

        private static string Wrap(string body) => Header + body + Footer;

        [Fact]
        public async Task DropColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG001:DropColumn|}(name: ""Notes"", table: ""Orders"");"));
        }

        [Fact]
        public async Task DropTable_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG004:DropTable|}(name: ""LegacyAudit"");"));
        }

        [Fact]
        public async Task CreateIndex_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG002:CreateIndex|}(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId"");"));
        }

        [Fact]
        public async Task AlterColumn_is_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG003:AlterColumn<string>|}(name: ""Status"", table: ""Orders"");"));
        }

        [Fact]
        public async Task Positional_arguments_are_understood()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.{|MIG001:DropColumn|}(""Notes"", ""Orders"");"));
        }

        [Fact]
        public async Task AddColumn_is_not_flagged()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"migrationBuilder.AddColumn<string>(""Notes"", ""Orders"");"));
        }

        [Fact]
        public async Task Reviewed_marker_suppresses_the_diagnostic()
        {
            await Verify.AnalyzerAsync(Wrap(
                @"// migration-safety:reviewed column is empty in every environment
        migrationBuilder.DropColumn(""Notes"", ""Orders"");"));
        }

        [Fact]
        public async Task Unrelated_type_named_similarly_is_ignored()
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
