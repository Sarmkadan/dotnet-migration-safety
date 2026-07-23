namespace MigrationSafety.Analyzers
{
    /// <summary>
    /// Stable identifiers for every diagnostic this package can raise.
    /// Keep these frozen once shipped - suppression files reference them by string.
    /// </summary>
    public static class DiagnosticIds
    {
        public const string DropColumn = "MIG001";
        public const string NonConcurrentIndex = "MIG002";
        public const string TableRewrite = "MIG003";
        public const string DropTable = "MIG004";
        public const string AddNotNullColumn = "MIG005";
        public const string RenameColumn = "MIG006";

        public const string Category = "MigrationSafety";
    }

    /// <summary>
    /// Well-known annotation names used by database providers to indicate online/concurrent operations.
    /// </summary>
    public static class WellKnownAnnotations
    {
        /// <summary>
        /// Npgsql annotation for creating indexes concurrently.
        /// </summary>
        public const string NpgsqlCreateIndexConcurrently = "Npgsql:CreateIndexConcurrently";

        /// <summary>
        /// SQL Server annotation for creating indexes online.
        /// </summary>
        public const string SqlServerCreateIndexOnline = "SqlServer:CreateIndexOnline";

        /// <summary>
        /// SQL Server annotation for instant index rebuilds.
        /// </summary>
        public const string SqlServerCreateIndexAlgorithm = "SqlServer:CreateIndexAlgorithm";

        /// <summary>
        /// PostgreSQL annotation for instant index rebuilds.
        /// </summary>
        public const string NpgsqlCreateIndexAlgorithm = "Npgsql:CreateIndexAlgorithm";

        /// <summary>
        /// MySQL annotation for online index creation.
        /// </summary>
        public const string MySqlCreateIndexOnline = "MySql:CreateIndexOnline";
    }
}