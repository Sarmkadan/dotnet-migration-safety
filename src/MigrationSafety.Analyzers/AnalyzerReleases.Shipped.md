; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|---------------------------------------
MIG001  | MigrationSafety | Warning | DropColumn is destructive
MIG002  | MigrationSafety | Warning | CreateIndex takes a blocking lock
MIG003  | MigrationSafety | Warning | AlterColumn rewrites the table
