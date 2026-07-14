namespace MigrationSafety.Analyzers.Tests
{
    /// <summary>
    /// The analyzer matches EF Core's MigrationBuilder by simple type name, so the tests do not
    /// need a full EF Core reference. This minimal stub carries the same method surface the rules
    /// look at and is prepended to every piece of test source.
    /// </summary>
    internal static class MigrationBuilderStub
    {
        public const string Source = @"
namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class OperationBuilder<T> { }
    public class AddColumnOperation { }
    public class CreateIndexOperation { }
    public class AlterColumnOperation { }
    public class MigrationBuilder
    {
        public OperationBuilder<AddColumnOperation> AddColumn<TColumn>(string name, string table) => null;
        public object DropColumn(string name, string table) => null;
        public object DropTable(string name) => null;
        public OperationBuilder<CreateIndexOperation> CreateIndex(string name, string table, string column) => null;
        public OperationBuilder<AlterColumnOperation> AlterColumn<TColumn>(string name, string table) => null;
        public void Sql(string sql) { }
    }
}
";
    }
}
