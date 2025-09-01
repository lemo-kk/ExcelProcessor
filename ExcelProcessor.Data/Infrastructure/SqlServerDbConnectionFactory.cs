using System;
using System.Data.Common;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class SqlServerDbConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;
		public SqlServerDbConnectionFactory(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}
		public DbConnection CreateConnection()
		{
			return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
		}
	}
} 