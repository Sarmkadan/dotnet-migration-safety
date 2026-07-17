using System.Threading.Tasks;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Contains tests for the code‑fix provider that adds a reviewed marker comment to migration operations.
    /// </summary>
    public class CodeFixTests
    {
        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above a <c>DropColumn</c> call.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
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
