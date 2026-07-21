using System;
using System.Linq;
using System.Reflection;
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
    /// identically‑named methods on unrelated types.
    /// </summary>
    public class MigrationBuilderCallsTests
    {
        /// <summary>
        /// Verifies that a method defined on the <c>MigrationBuilderStub</c> (the test stub that mimics
        /// EF Core's <c>MigrationBuilder</c>) is recognised as a MigrationBuilder method.
        /// </summary>
        [Fact]
        public void MigrationBuilderMethod_is_recognized()
        {
            const string source = @"
using MigrationSafety.Analyzers.Tests;

public class TestClass
{
    public void Test()
    {
        var mb = new MigrationBuilderStub();
        mb.DropColumn(""Notes"", ""Orders"");
    }
}
";

            // Build a compilation that includes the test source and references the current test assembly
            // (which contains MigrationBuilderStub) as well as the assembly that defines MigrationBuilderCalls.
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MigrationBuilderCalls).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MigrationBuilderStub).Assembly.Location),
                // Reference to System.Runtime (required for .NET 5+)
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "MigrationBuilderCallsTests_Compilation",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Retrieve the method symbol for the invoked DropColumn call.
            var model = compilation.GetSemanticModel(syntaxTree);
            var invocation = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First();

            var symbolInfo = model.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            Assert.NotNull(methodSymbol);

            // The method should be recognised as a MigrationBuilder method.
            bool result = MigrationBuilderCalls.IsMigrationBuilderMethod(methodSymbol);
            Assert.True(result, "DropColumn on MigrationBuilderStub should be recognised as a MigrationBuilder method.");
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

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MigrationBuilderCalls).Assembly.Location),
                // Reference to System.Runtime (required for .NET 5+)
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "MigrationBuilderCallsTests_Unrelated_Compilation",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var model = compilation.GetSemanticModel(syntaxTree);
            var invocation = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First();

            var symbolInfo = model.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            Assert.NotNull(methodSymbol);

            // The method should **not** be recognised as a MigrationBuilder method.
            bool result = MigrationBuilderCalls.IsMigrationBuilderMethod(methodSymbol);
            Assert.False(result, "DropColumn on an unrelated type should not be recognised as a MigrationBuilder method.");
        }
    }
}
