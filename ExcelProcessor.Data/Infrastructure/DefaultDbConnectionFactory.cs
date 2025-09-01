using System;
using System.Data.Common;
using System.Data.SQLite;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class DefaultDbConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;
		public DefaultDbConnectionFactory(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}
		public DbConnection CreateConnection()
		{
			return new SQLiteConnection(_connectionString);
		}
	}
} 