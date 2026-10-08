using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace MigrationSafety.Analyzers.Tests
{
    public class MigrationSafetyAnalyzerTests
    {
        [Fact]
        public void Analyzer_SupportedDiagnostics_Is_Not_Empty()
        {
            var analyzer = new MigrationSafetyAnalyzer();
            var supported = analyzer.SupportedDiagnostics;

            Assert.NotEmpty(supported);
            Assert.Contains(supported, d => d.Id == DiagnosticIds.DropColumn);
        }
    }
}
