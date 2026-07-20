using Microsoft.CodeAnalysis;

namespace MigrationSafety.Analyzers
{
	/// <summary>
	/// Central table of <see cref="DiagnosticDescriptor"/> instances so the analyzer and
	/// the code-fix provider agree on ids, severities and help links.
	/// </summary>
	internal static class Descriptors
	{
		private const string HelpUriBase = "https://github.com/sarmkadan/dotnet-migration-safety/blob/main/docs/rules/";

		internal static readonly DiagnosticDescriptor DropColumn = new DiagnosticDescriptor(
			id: DiagnosticIds.DropColumn,
			title: "Migration drops a column",
			messageFormat: "DropColumn('{0}') on table '{1}' is destructive - data in this column is lost permanently",
			category: DiagnosticIds.Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Dropping a column deletes data irreversibly and breaks older application versions still reading it during a rolling deploy. Prefer an expand/contract migration.",
			helpLinkUri: HelpUriBase + "MIG001.md");

		internal static readonly DiagnosticDescriptor NonConcurrentIndex = new DiagnosticDescriptor(
			id: DiagnosticIds.NonConcurrentIndex,
			title: "Index is created without the concurrent option",
			messageFormat: "CreateIndex('{0}') takes a write lock on '{1}' for the whole build - use a concurrent index build on large tables",
			category: DiagnosticIds.Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "A plain CREATE INDEX holds a lock that blocks writes until the index is fully built. On large tables this can stall the application for minutes.",
			helpLinkUri: HelpUriBase + "MIG002.md");

		internal static readonly DiagnosticDescriptor TableRewrite = new DiagnosticDescriptor(
			id: DiagnosticIds.TableRewrite,
			title: "Column change forces a full table rewrite",
			messageFormat: "AlterColumn('{0}') on '{1}' changes the stored type and rewrites every row under lock",
			category: DiagnosticIds.Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Changing a column type (or making it non-nullable) rewrites the whole table while holding an exclusive lock. Split into additive steps and backfill out of band.",
			helpLinkUri: HelpUriBase + "MIG003.md");

		internal static readonly DiagnosticDescriptor DropTable = new DiagnosticDescriptor(
			id: DiagnosticIds.DropTable,
			title: "Migration drops a table",
			messageFormat: "DropTable('{0}') is destructive - every row is lost and dependent code will fail",
			category: DiagnosticIds.Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Dropping a table deletes all its data and cannot be rolled forward. Retire tables in a separate, clearly reviewed migration once nothing references them.",
			helpLinkUri: HelpUriBase + "MIG004.md");

		internal static readonly DiagnosticDescriptor AddNotNullColumn = new DiagnosticDescriptor(
			id: DiagnosticIds.AddNotNullColumn,
			title: "Adding a non-nullable column without a default value",
			messageFormat: "AddColumn with nullable:false requires a default value or defaultValueSql for populated tables - consider making the column nullable or providing a default",
			category: DiagnosticIds.Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Adding a non-nullable column to a populated table without a default value will fail because the database cannot determine what value to use for existing rows. Either make the column nullable, add a default value, or use defaultValueSql to provide a SQL expression.",
			helpLinkUri: HelpUriBase + "MIG005.md");

        internal static readonly DiagnosticDescriptor RenameColumn = new DiagnosticDescriptor(
            id: DiagnosticIds.RenameColumn,
            title: "Rename operation breaks rolling deploys",
            messageFormat: "Rename operation on '{0}' to '{1}' breaks rolling deploys - old app version still queries old name",
            category: DiagnosticIds.Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Renaming a column or table changes the schema name, which can break rolling deployments where older application versions still reference the previous name. Review and coordinate deployments carefully.",
            helpLinkUri: HelpUriBase + "MIG006.md");
	}
}
