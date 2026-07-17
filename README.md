# Migration Safety Analyzer

A Roslyn analyzer that flags destructive or lock-heavy EF Core migrations at build
time, before they reach production. It reads the `migrationBuilder.*` calls in your
generated migration files and warns on the operations that most often cause outages
or data loss.

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

## License

MIT. See [LICENSE](LICENSE).
