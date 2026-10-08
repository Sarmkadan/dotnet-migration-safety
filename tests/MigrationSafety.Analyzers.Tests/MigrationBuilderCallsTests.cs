using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using MigrationSafety.Analyzers;

namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MigrationBuilderCalls.IsMigrationBuilderMethod"/>. The tests compile
    /// small snippets of code with Roslyn, retrieve the <see cref="IMethodSymbol"/> for the invoked
    /// method and verify that the helper correctly identifies MigrationBuilder methods and rejects
    /// identically-named methods on unrelated types.
    /// </summary>
    public class MigrationBuilderCallsTests
    {
        private static (IMethodSymbol Symbol, string Diagnostics) GetMethodSymbol(string source, string methodName)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Use Basic.Reference.Assemblies or collect from the runtime directory
            var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "MigrationBuilderCallsTests_Compilation",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diags = string.Join("\n", compilation.GetDiagnostics().Select(d => d.ToString()));

            var model = compilation.GetSemanticModel(syntaxTree);
            var invocation = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First(i => i.ToString().Contains(methodName));

            var symbolInfo = model.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol method)
                return (method, diags);
            if (symbolInfo.CandidateSymbols.Length > 0)
                return (symbolInfo.CandidateSymbols[0] as IMethodSymbol, diags);
            return (null, diags);
        }

        /// <summary>
        /// Verifies that a method defined on a type named MigrationBuilder
        /// is recognised as a MigrationBuilder method.
        /// </summary>
        [Fact]
        public void MigrationBuilderMethod_is_recognized()
        {
            var source = @"
using Microsoft.EntityFrameworkCore.Migrations;
" + MigrationBuilderStub.Source + @"
public class TestClass
{
    public void Test()
    {
        var mb = new MigrationBuilder();
        mb.DropColumn(""Notes"", ""Orders"");
    }
}
";

            var (methodSymbol, diagnostics) = GetMethodSymbol(source, "DropColumn");
            Assert.True(methodSymbol != null, $"Method symbol is null. Diagnostics:\n{diagnostics}");

            bool result = MigrationBuilderCalls.IsMigrationBuilderMethod(methodSymbol);
            Assert.True(result, "DropColumn on MigrationBuilder should be recognised as a MigrationBuilder method.");
        }

        /// <summary>
        /// Verifies that a method with the same name as a MigrationBuilder operation but defined on an
        /// unrelated type is **not** recognised as a MigrationBuilder method.
        /// </summary>
        [Fact]
        public void UnrelatedMethod_is_not_recognized()
        {
            const string source = @"
public class NotABuilder
{
    public void DropColumn(string name, string table) { }
}

public class TestClass
{
    public void Test()
    {
        var nb = new NotABuilder();
        nb.DropColumn(""Notes"", ""Orders"");
    }
}
";

            var (methodSymbol, _) = GetMethodSymbol(source, "DropColumn");
            Assert.NotNull(methodSymbol);

            bool result = MigrationBuilderCalls.IsMigrationBuilderMethod(methodSymbol);
            Assert.False(result, "DropColumn on an unrelated type should not be recognised as a MigrationBuilder method.");
        }
    }
}
