using System.Threading.Tasks;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    public class CodeFixTests
    {
        [Fact]
        public async Task DropColumn_gets_a_reviewed_marker()
        {
            const string source = @"
using Microsoft.EntityFrameworkCore.Migrations;

public class SomeMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.{|MIG001:DropColumn|}(name: ""Notes"", table: ""Orders"");
    }
}
";

            const string fixedSource = @"
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

            await Verify.CodeFixAsync(source, fixedSource);
        }
    }
}
