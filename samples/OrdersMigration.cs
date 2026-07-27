using Microsoft.EntityFrameworkCore.Migrations;

namespace Sample.Migrations
{
    /// <summary>
    /// A migration deliberately full of risky operations so you can see the analyzer light up.
    /// Reference the MigrationSafety.Analyzers package from a project that contains this file and
    /// build it - each flagged line below will produce an MIGxxx warning.
    /// </summary>
    public partial class ReworkOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MIG001 - loses the data in Notes
            migrationBuilder.DropColumn(name: "Notes", table: "Orders");

            // MIG003 - rewrites the whole Orders table under lock
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                nullable: false);

            // MIG002 - blocks writes on Orders while the index builds
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            // MIG004 - drops a whole table
            migrationBuilder.DropTable(name: "LegacyAudit");

            // Reviewed and accepted - no warning here
            // migration-safety:reviewed Drafts is empty in every environment
            migrationBuilder.DropColumn(name: "Draft", table: "Orders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }

    /// <summary>
    /// A second migration class demonstrating every diagnostic with explicit expected annotations.
    /// </summary>
    public partial class BadOrdersMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // expected: MIG001
            migrationBuilder.DropColumn(name: "Notes", table: "Orders");

            // expected: MIG002
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            // expected: MIG003
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                nullable: false);

            // expected: MIG004
            migrationBuilder.DropTable(name: "LegacyAudit");

            // migration-safety:reviewed Reason for suppression
            migrationBuilder.DropColumn(name: "Draft", table: "Orders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
