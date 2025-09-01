using System;
using System.Data.Common;
using ExcelProcessor.Core.Interfaces;
using Npgsql;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class PostgreSqlDbConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;
		public PostgreSqlDbConnectionFactory(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}
		public DbConnection CreateConnection()
		{
			return new NpgsqlConnection(_connectionString);
		}
	}
} 