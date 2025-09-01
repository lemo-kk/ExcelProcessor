using System.Collections.Generic;

namespace ExcelProcessor.Core.Interfaces
{
	/// <summary>
	/// Database dialect for SQL generation and type mapping.
	/// </summary>
	public interface ISqlDialect
	{
		string QuoteIdentifier(string identifier);
		string Parameterize(string name);
		string BuildCreateTable(string tableName, IDictionary<string, string> columns);
		string BuildInsert(string tableName, IReadOnlyList<string> columnNames);
		string BuildTruncateOrDeleteAll(string tableName);
		string GetExistsTableSql(string tableName);
		string MapType(string neutralType);
	}
} 