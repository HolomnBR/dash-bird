namespace FirebirdApi.Models
{
    public class TableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DatabaseConnectionTest
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DatabaseId { get; set; } = string.Empty;
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    }

	public class ColumnSchema
	{
		public string ColumnName { get; set; } = string.Empty;
		public string DataType { get; set; } = string.Empty;
		public int? Length { get; set; }
		public int? Precision { get; set; }
		public int? Scale { get; set; }
		public bool IsNullable { get; set; }
		public bool IsPrimaryKey { get; set; }
		public string Default { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}

	public class TableSchema
	{
		public string TableName { get; set; } = string.Empty;
		public List<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();
	}
}
