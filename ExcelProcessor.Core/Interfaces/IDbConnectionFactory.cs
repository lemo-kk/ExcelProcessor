using System.Data.Common;

namespace ExcelProcessor.Core.Interfaces
{
	/// <summary>
	/// Factory for creating database connections based on configuration.
	/// </summary>
	public interface IDbConnectionFactory
	{
		/// <summary>
		/// Create a database connection. The caller is responsible for disposing it.
		/// </summary>
		DbConnection CreateConnection();
	}
} 