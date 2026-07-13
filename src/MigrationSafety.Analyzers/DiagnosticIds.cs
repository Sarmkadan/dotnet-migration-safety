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

        public const string Category = "MigrationSafety";
    }
}
