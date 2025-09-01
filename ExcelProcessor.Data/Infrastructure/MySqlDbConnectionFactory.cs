using System;
using System.Data.Common;
using ExcelProcessor.Core.Interfaces;
using MySql.Data.MySqlClient;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class MySqlDbConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;
		public MySqlDbConnectionFactory(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}
		public DbConnection CreateConnection()
		{
			return new MySqlConnection(_connectionString);
		}
	}
} 