using System.Collections.Generic;
using System.Linq;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class PostgreSqlDialect : ISqlDialect
	{
		public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";
		public string Parameterize(string name) => $"@{name}";
		public string BuildCreateTable(string tableName, IDictionary<string, string> columns)
		{
			var cols = string.Join(", ", columns.Select(kv => $"{QuoteIdentifier(kv.Key)} {MapType(kv.Value)}"));
			return $"CREATE TABLE {QuoteIdentifier(tableName)} (Id SERIAL PRIMARY KEY, {cols})";
		}
		public string BuildInsert(string tableName, IReadOnlyList<string> columnNames)
		{
			var cols = string.Join(", ", columnNames.Select(QuoteIdentifier));
			var pars = string.Join(", ", columnNames.Select(Parameterize));
			return $"INSERT INTO {QuoteIdentifier(tableName)} ({cols}) VALUES ({pars})";
		}
		public string BuildTruncateOrDeleteAll(string tableName) => $"TRUNCATE TABLE {QuoteIdentifier(tableName)}";
		public string GetExistsTableSql(string tableName) => "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @TableName";
		public string MapType(string neutralType)
		{
			switch (neutralType.ToUpper())
			{
				case "INT": return "INTEGER";
				case "DECIMAL(10,2)":
				case "DECIMAL(15,2)": return neutralType.ToUpper();
				case "DATE": return "DATE";
				case "DATETIME": return "TIMESTAMP";
				case "TEXT": return "TEXT";
				case "VARCHAR(50)":
				case "VARCHAR(100)":
				case "VARCHAR(200)": return neutralType.ToUpper();
				default: return "TEXT";
			}
		}
	}
} 