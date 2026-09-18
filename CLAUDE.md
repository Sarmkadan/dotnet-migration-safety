# CLAUDE.md

Roslyn analyzer (NuGet `MigrationSafety.Analyzers`) that flags destructive or lock-heavy EF Core migration calls (`DropColumn`, `DropTable`, `CreateIndex`, `AlterColumn`) at build time and ships a "mark as reviewed" code fix.

## Build

```sh
dotnet build MigrationSafety.slnx
dotnet pack src/MigrationSafety.Analyzers.Package -c Release   # produces the NuGet package
```

- Requires .NET 10 SDK (tests target `net10.0`; analyzer projects target `netstandard2.0`).
- Solution file is `.slnx`. `Directory.Build.props` sets `Nullable=enable`, `ImplicitUsings=disable`, `LangVersion=latest`, `EnforceExtendedAnalyzerRules=true`.
- Only `MigrationSafety.Analyzers.Package` is packable; it copies both analyzer DLLs into `analyzers/dotnet/cs` via the `PackAnalyzerAssemblies` target.

## Test

```sh
dotnet test MigrationSafety.slnx
dotnet test tests/MigrationSafety.Analyzers.Tests --filter "FullyQualifiedName~AnalyzerTests"
```

- xUnit + `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` / `CodeFix.Testing`.
- Tests go through `Verify.AnalyzerAsync` / `Verify.CodeFixAsync` (`tests/.../Verifiers.cs`), which prepend `MigrationBuilderStub.Source` so no EF Core reference is needed.
- Expected diagnostics are declared with `Verify.Diagnostic(Descriptors.X).WithLocation(...)` on the method-name token.

## Lint / format

No `.editorconfig` and no formatter configured. `TreatWarningsAsErrors` is off. Use `dotnet format` only if asked; do not introduce style-only churn.

## Layout and entry points

```
src/MigrationSafety.Analyzers/            analyzer (netstandard2.0)
  MigrationSafetyAnalyzer.cs              the single DiagnosticAnalyzer; dispatches on method name
  Descriptors.cs                          all DiagnosticDescriptors (ids, severity, help links)
  DiagnosticIds.cs                        MIG001..MIG006, MIG999 string constants (frozen once shipped)
  MigrationBuilderCalls.cs                IsMigrationBuilderMethod, GetStringArgument helpers
  SuppressionComment.cs                   parses "// migration-safety:reviewed <reason>" trivia
  ReviewRecord.cs                         structured review data behind the marker
  AnalyzerReleases.{Shipped,Unshipped}.md release tracking (required by RS2000-series rules)
src/MigrationSafety.Analyzers.CodeFixes/  MarkReviewedCodeFixProvider + ReviewRecordSerializer (System.Text.Json)
src/MigrationSafety.Analyzers.Package/    packaging shim, no code
tests/MigrationSafety.Analyzers.Tests/    xUnit tests, Verifiers.cs, MigrationBuilderStub.cs
samples/OrdersMigration.cs                migration that trips every rule
docs/ARCHITECTURE.md                      design decisions and how to add a rule
docs/rules/MIG00x.md                      one page per rule; these are the descriptors' help links
docs/suppression_comment_grammar.md       accepted marker syntax
```

Stray files: `src/MigrationSafetyAnalyzer.cs`, `tests/MigrationSafetyAnalyzerTests.cs`, `tests/.../AnalyzerTests.cs.backup` and the root file named `}` are leftovers that sit outside every project directory and are not compiled. Do not build on them.

## Conventions

- Namespace is `MigrationSafety.Analyzers` for all `src` projects; tests use `MigrationSafety.Analyzers.Tests`. `InternalsVisibleTo` exposes internals to CodeFixes and Tests.
- Diagnostic ids: `MIG` + 3 digits, category `MigrationSafety`, default severity Warning. Never renumber a shipped id; add new ones to `DiagnosticIds`, `Descriptors`, `AnalyzerReleases.Unshipped.md`, and a `docs/rules/MIGxxx.md` page.
- Adding a rule: new descriptor in `Descriptors`, a case in `MigrationSafetyAnalyzer`, analyzer tests (positive, negative, suppressed), rule doc. The code fix is generic and needs no change.
- Match `MigrationBuilder` by type name, not assembly, so the analyzer works without an EF Core reference (and in tests via the stub).
- Report diagnostics on the method-name identifier token, not the whole invocation.
- Suppression is comment-based only (`// migration-safety:reviewed ...`), case-insensitive and whitespace-tolerant; `#pragma` is intentionally not the recommended path.
- Explicit `using` directives (implicit usings disabled); nullable annotations required.
- XML doc comments on public and internal types.
- Commit messages: conventional prefix (`docs:`, `refactor:`, `chore:`), imperative, no trailer lines.
