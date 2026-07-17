# Architecture

## Overview

MigrationSafety is a Roslyn analyzer package that inspects EF Core migration source
files at compile time and warns on the four operations that most often cause outages
or irreversible data loss in production:

| Id | Trigger | Descriptor |
|----|---------|------------|
| MIG001 | `migrationBuilder.DropColumn(...)` | `Descriptors.DropColumn` |
| MIG002 | `migrationBuilder.CreateIndex(...)` | `Descriptors.NonConcurrentIndex` |
| MIG003 | `migrationBuilder.AlterColumn<T>(...)` | `Descriptors.TableRewrite` |
| MIG004 | `migrationBuilder.DropTable(...)` | `Descriptors.DropTable` |

There is no runtime component: the package ships two analyzer assemblies under
`analyzers/dotnet/cs` and contributes nothing to the consumer's output.

## Projects

```
src/MigrationSafety.Analyzers/           the analyzer (netstandard2.0)
src/MigrationSafety.Analyzers.CodeFixes/ the "mark as reviewed" code fix (netstandard2.0)
src/MigrationSafety.Analyzers.Package/   NuGet packaging shim (no code of its own)
tests/MigrationSafety.Analyzers.Tests/   xUnit + Microsoft.CodeAnalysis.Testing
samples/OrdersMigration.cs               a migration that trips every rule
docs/rules/MIG00x.md                     one page per rule (the descriptors' help links)
```

### MigrationSafety.Analyzers

- `MigrationSafetyAnalyzer` - the single `DiagnosticAnalyzer`. Registers one syntax
  node action for `InvocationExpression` and dispatches on the resolved method name
  (`DropColumn`, `DropTable`, `CreateIndex`, `AlterColumn`). The diagnostic is
  reported on the method-name token of the member access so the squiggle lands on
  the operation itself, not the whole statement.
- `Descriptors` - the central table of `DiagnosticDescriptor`s; ids, severities and
  help links live in exactly one place so the analyzer and code fix cannot drift.
- `DiagnosticIds` - string constants for the ids. Frozen once shipped, because
  suppression files and `.editorconfig` entries reference them by string.
- `MigrationBuilderCalls` - shared helpers: `IsMigrationBuilderMethod` (type-name
  match, see below) and `GetStringArgument`, which resolves a string-literal
  argument whether it was passed positionally or by name. Non-literal arguments
  (variables, interpolated strings) resolve to `null` and the message falls back
  to `?` rather than suppressing the diagnostic.
- `SuppressionComment` - recognises the inline
  `// migration-safety:reviewed <reason>` marker in the leading trivia of the
  statement containing the call (case-insensitive, single- or multi-line comment).

### MigrationSafety.Analyzers.CodeFixes

`MarkReviewedCodeFixProvider` offers exactly one fix for all four diagnostics:
prepend the reviewed marker (with the diagnostic id and a `TODO` placeholder for
the reason) to the flagged statement. It deliberately does not rewrite the
migration - deciding how to make an operation safe is a human call; the fix only
records that the risk was seen. It supports fix-all via the standard batch fixer.

### MigrationSafety.Analyzers.Package

A `netstandard2.0` project with `IncludeBuildOutput=false` whose only job is
packing. A custom `PackAnalyzerAssemblies` target places both DLLs under
`analyzers/dotnet/cs`. `DevelopmentDependency=true` plus
`SuppressDependenciesWhenPacking` keep the package free of runtime dependencies
for consumers.

## Key design decisions

**Syntax-tree analysis with a semantic-model name match.** The analyzer resolves
the invocation symbol and accepts any instance method whose containing type is
named `MigrationBuilder` (`MigrationBuilderCalls.IsMigrationBuilderMethod`). It
does not check the full metadata name `Microsoft.EntityFrameworkCore.Migrations.
MigrationBuilder`. Trade-off: no compile-time dependency on any EF Core version
(the analyzer and the tests work against a stub type), at the cost of also
flagging a hypothetical unrelated type with the same name. For this domain that
false-positive risk is negligible.

**Comment-based suppression instead of `#pragma`.** Migrations are scaffolded
files that get regenerated and reviewed in diffs; a `#pragma warning disable`
block silences everything in scope and carries no justification. The
`// migration-safety:reviewed <reason>` marker is per-statement, keeps the reason
in the diff for the next reviewer, and is what the bundled code fix inserts.

**Warnings by default, errors opt-in.** All four descriptors default to
`DiagnosticSeverity.Warning`; teams promote them with
`dotnet_diagnostic.MIGxxx.severity = error` in `.editorconfig`. This lets the
package be adopted incrementally on codebases with existing migrations.

**No row-count heuristics.** MIG002 flags every `CreateIndex` because a compiler
cannot know table sizes. The reviewed marker is the intended escape hatch for
genuinely small tables.

**Generated-code analysis is enabled on purpose.** Migration files are scaffolded
code and are the entire point of this analyzer, so `Initialize` configures
`GeneratedCodeAnalysisFlags.Analyze | ReportDiagnostics` rather than the usual
`None`.

## Data flow

1. Roslyn hands the analyzer every `InvocationExpressionSyntax` in the compilation.
2. Non-member-access invocations and calls whose resolved symbol is not a
   `MigrationBuilder` instance method are discarded early.
3. If the containing statement carries the reviewed marker, the call is skipped.
4. The method name selects a descriptor; `GetStringArgument` pulls the `name` /
   `table` literals for the message (falling back to `?`); the diagnostic is
   reported at the method-name token.
5. In the IDE, the code-fix provider maps any MIG diagnostic back to its statement
   and offers to insert the reviewed marker, re-using the statement's existing
   indentation.

## Extension points

Adding a rule is four mechanical steps:

1. New id constant in `DiagnosticIds` and a matching `DiagnosticDescriptor` in
   `Descriptors` (plus a `docs/rules/MIGxxx.md` page for the help link).
2. A `case` in `MigrationSafetyAnalyzer.AnalyzeInvocation` and the descriptor added
   to `SupportedDiagnostics`.
3. The id added to `MarkReviewedCodeFixProvider.FixableDiagnosticIds` if the
   reviewed marker should apply (it should - the fix is generic).
4. Tests in `AnalyzerTests` / `CodeFixTests`, extending `MigrationBuilderStub` with
   the new method signature if needed.

New diagnostics must also be recorded in `AnalyzerReleases.Unshipped.md`
(`EnforceExtendedAnalyzerRules` is on, so RS2000-series checks enforce this).

## Testing

Tests use `Microsoft.CodeAnalysis.Testing`'s `CSharpAnalyzerTest` /
`CSharpCodeFixTest` with markup spans (`{|MIG001:DropColumn|}`). Because matching
is by simple type name, the tests inject `MigrationBuilderStub` - a minimal
`Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder` with just the methods
the rules inspect - instead of referencing EF Core.

## Known limitations

- Raw `migrationBuilder.Sql(...)` is not parsed; only the strongly typed builder
  API is inspected.
- MIG002 cannot distinguish concurrent from non-concurrent index builds (that is a
  provider-level annotation), so it flags all `CreateIndex` calls.
- MIG003 flags every `AlterColumn`, including changes (e.g. widening a max length)
  that some providers execute without a rewrite.
- Argument values are only extracted from plain string literals; constants or
  interpolated strings render as `?` in the message (the diagnostic still fires).
- The reviewed marker is matched by substring in leading trivia, so it must sit on
  (or directly above) the flagged statement, not at file level.
