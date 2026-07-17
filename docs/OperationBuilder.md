# OperationBuilder

`OperationBuilder<T>` is a generic fluent API for constructing database migration operations in a type-safe manner. It provides methods to add columns, create indexes, alter columns, drop tables or columns, and execute raw SQL, each returning a builder scoped to the specific operation type for further configuration.

## API

### `OperationBuilder<T>`

The base generic builder class that wraps a migration operation of type `T`. It serves as the foundation for all specific operation builders and provides the fluent chaining mechanism.

### `AddColumnOperation`

Represents a migration operation that adds a new column to an existing table. Returned by `AddColumn<TColumn>()` and can be further configured through the returned `OperationBuilder<AddColumnOperation>`.

### `CreateIndexOperation`

Represents a migration operation that creates a new index on a table. Returned by `CreateIndex` and can be further configured through the returned `OperationBuilder<CreateIndexOperation>`.

### `AlterColumnOperation`

Represents a migration operation that alters an existing column's definition. Returned by `AlterColumn<TColumn>()` and can be further configured through the returned `OperationBuilder<AlterColumnOperation>`.

### `OperationBuilder<AddColumnOperation> AddColumn<TColumn>`

Adds a column of type `TColumn` to the current table context.

- **Parameters:** None (the column type is specified via the generic parameter `TColumn`).
- **Returns:** An `OperationBuilder<AddColumnOperation>` that allows further configuration of the newly added column (e.g., nullable, default value).
- **Throws:** May throw if the column type is not supported by the target database provider or if the operation is incompatible with the current migration state.

### `object DropColumn`

Initiates the removal of a column from the current table context.

- **Returns:** An `object` representing the drop column operation. The returned object allows specifying the column name and other drop options.
- **Throws:** May throw if the column does not exist or if the operation is invalid for the current table state.

### `object DropTable`

Initiates the removal of a table from the database schema.

- **Returns:** An `object` representing the drop table operation. The returned object allows specifying the table name and any constraints.
- **Throws:** May throw if the table does not exist or if foreign key constraints prevent the drop.

### `OperationBuilder<CreateIndexOperation> CreateIndex`

Initiates the creation of a new index on the current table context.

- **Returns:** An `OperationBuilder<CreateIndexOperation>` that allows further configuration of the index (e.g., columns, unique, name).
- **Throws:** May throw if the index name conflicts with an existing index or if the specified columns are invalid.

### `OperationBuilder<AlterColumnOperation> AlterColumn<TColumn>`

Initiates an alteration of an existing column to the specified type `TColumn`.

- **Parameters:** `TColumn` — the target type for the column alteration.
- **Returns:** An `OperationBuilder<AlterColumnOperation>` that allows further configuration of the alteration (e.g., nullable, new default value).
- **Throws:** May throw if the column does not exist, if the type conversion is not supported, or if the alteration would cause data loss.

### `void Sql(string sql)`

Executes a raw SQL statement as part of the migration.

- **Parameters:**
  - `sql` — the raw SQL string to execute. Must not be null or empty.
- **Returns:** Nothing.
- **Throws:** May throw if the SQL string is null or empty, or if the SQL syntax is invalid for the target database.

## Usage

### Example 1: Adding a Column and Creating an Index

```csharp
migrationBuilder.AlterTable("Users", table =>
{
    table.AddColumn<string>("Email")
        .IsNullable(false)
        .HasMaxLength(256);

    table.CreateIndex("IX_Users_Email")
        .IsUnique()
        .OnColumn("Email");
});
```

### Example 2: Altering a Column and Executing Raw SQL

```csharp
migrationBuilder.AlterTable("Orders", table =>
{
    table.AlterColumn<decimal>("TotalAmount")
        .HasColumnType("decimal(18,4)")
        .IsNullable(false);

    table.Sql("UPDATE Orders SET TotalAmount = 0 WHERE TotalAmount IS NULL");
});
```

## Notes

- **Thread Safety:** `OperationBuilder<T>` and its derived types are not thread-safe. They are designed for sequential use within a single migration step. Concurrent access from multiple threads will result in undefined behavior.
- **Edge Cases:**
  - Calling `DropColumn` or `DropTable` on a non-existent object will throw at runtime when the migration is applied, not during builder configuration.
  - `AlterColumn<TColumn>` requires that the column already exists; otherwise, an exception is thrown during migration execution.
  - The `Sql()` method accepts arbitrary SQL, which bypasses the safety checks provided by the typed operation methods. Use with caution, especially when the SQL contains data-modifying statements.
  - Operations are accumulated in order and executed sequentially. Reordering operations after they have been added is not supported through the builder API.
  - The generic type parameter `TColumn` in `AddColumn<TColumn>` and `AlterColumn<TColumn>` must map to a valid database type supported by the provider, or the migration will fail at execution time.
