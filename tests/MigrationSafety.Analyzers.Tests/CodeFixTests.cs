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

        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above a <c>CreateIndex</c> call (MIG002).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task NonConcurrentIndex_gets_a_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG002:CreateIndex|}(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId""); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { // migration-safety:reviewed (MIG002) TODO: document why this is safe migrationBuilder.CreateIndex(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId""); } } "; await Verify.CodeFixAsync(source, fixedSource);
        }

        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above an <c>AlterColumn</c> call (MIG003).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task TableRewrite_gets_a_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG003:AlterColumn|}(name: ""Status"", table: ""Orders"", nullable: false); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { // migration-safety:reviewed (MIG003) TODO: document why this is safe migrationBuilder.AlterColumn(name: ""Status"", table: ""Orders"", nullable: false); } } "; await Verify.CodeFixAsync(source, fixedSource);
        }

        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above a <c>DropTable</c> call (MIG004).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task DropTable_gets_a_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG004:DropTable|}(name: ""LegacyAudit""); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { // migration-safety:reviewed (MIG004) TODO: document why this is safe migrationBuilder.DropTable(name: ""LegacyAudit""); } } "; await Verify.CodeFixAsync(source, fixedSource);
        }

        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above an <c>AddColumn</c> call with not-null constraint (MIG005).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task AddNotNullColumn_gets_a_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG005:AddColumn|}(name: ""IsActive"", table: ""Users"", nullable: false); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { // migration-safety:reviewed (MIG005) TODO: document why this is safe migrationBuilder.AddColumn(name: ""IsActive"", table: ""Users"", nullable: false); } } "; await Verify.CodeFixAsync(source, fixedSource);
        }

        /// <summary>
        /// Verifies that the code‑fix inserts a reviewed‑marker comment above a <c>RenameColumn</c> call (MIG006).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task RenameColumn_gets_a_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG006:RenameColumn|}(name: ""OldName"", newName: ""NewName"", table: ""Users""); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { // migration-safety:reviewed (MIG006) TODO: document why this is safe migrationBuilder.RenameColumn(name: ""OldName"", newName: ""NewName"", table: ""Users""); } } "; await Verify.CodeFixAsync(source, fixedSource);
        }

        /// <summary>
        /// Verifies that the code‑fix can insert a reviewed‑marker comment as a trailing comment.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the verification finishes.</returns>
        [Fact]
        public async Task DropColumn_gets_a_trailing_reviewed_marker()
        {
            const string source = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.{|MIG001:DropColumn|}(name: ""Notes"", table: ""Orders""); } } "; const string fixedSource = @" using Microsoft.EntityFrameworkCore.Migrations; public class SomeMigration { public void Up(MigrationBuilder migrationBuilder) { migrationBuilder.DropColumn(name: ""Notes"", table: ""Orders""); // migration-safety:reviewed (MIG001) TODO: document why this is safe } } "; await Verify.CodeFixAsync(source, fixedSource);
        }
    }
}
