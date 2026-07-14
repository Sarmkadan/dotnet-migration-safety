using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Thin wrappers over the Roslyn testing harness that inject the MigrationBuilder stub in
    /// front of the sample source and expose the small surface the tests actually use.
    /// </summary>
    internal static class Verify
    {
        internal static DiagnosticResult Diagnostic(Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor)
            => new DiagnosticResult(descriptor);

        internal static async Task AnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<MigrationSafetyAnalyzer, DefaultVerifier>
            {
                TestCode = source,
            };
            test.TestState.Sources.Add(("MigrationBuilderStub.cs", MigrationBuilderStub.Source));
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        internal static async Task CodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<MigrationSafetyAnalyzer, MarkReviewedCodeFixProvider, DefaultVerifier>
            {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.TestState.Sources.Add(("MigrationBuilderStub.cs", MigrationBuilderStub.Source));
            test.FixedState.Sources.Add(("MigrationBuilderStub.cs", MigrationBuilderStub.Source));
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
