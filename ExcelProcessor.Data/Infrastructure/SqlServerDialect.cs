using System;
using System.Collections.Generic;
using System.Linq;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class SqlServerDialect : ISqlDialect
	{
		public string QuoteIdentifier(string identifier) => $"[{identifier}]";
		public string Parameterize(string name) => $"@{name}";
		public string BuildCreateTable(string tableName, IDictionary<string, string> columns)
		{
			var cols = string.Join(", ", columns.Select(kv => $"{QuoteIdentifier(kv.Key)} {MapType(kv.Value)}"));
			return $"CREATE TABLE {QuoteIdentifier(tableName)} (Id INT IDENTITY(1,1) PRIMARY KEY, {cols})";
		}
		public string BuildInsert(string tableName, IReadOnlyList<string> columnNames)
		{
			var cols = string.Join(", ", columnNames.Select(QuoteIdentifier));
			var pars = string.Join(", ", columnNames.Select(Parameterize));
			return $"INSERT INTO {QuoteIdentifier(tableName)} ({cols}) VALUES ({pars})";
		}
		public string BuildTruncateOrDeleteAll(string tableName) => $"TRUNCATE TABLE {QuoteIdentifier(tableName)}";
		public string GetExistsTableSql(string tableName) => "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
		public string MapType(string neutralType)
		{
			if (string.IsNullOrWhiteSpace(neutralType)) return "NVARCHAR(MAX)";
			switch (neutralType.ToUpper())
			{
				case "INT": return "INT";
				case "DECIMAL(10,2)":
				case "DECIMAL(15,2)": return neutralType.ToUpper();
				case "DATE": return "DATE";
				case "DATETIME": return "DATETIME2";
				case "TEXT": return "NVARCHAR(MAX)";
				default:
					if (neutralType.StartsWith("VARCHAR(", StringComparison.OrdinalIgnoreCase))
					{
						var sizePart = neutralType.Substring("VARCHAR".Length); // includes parentheses
						return $"NVARCHAR{sizePart}";
					}
					return "NVARCHAR(MAX)";
			}
		}
	}
} 