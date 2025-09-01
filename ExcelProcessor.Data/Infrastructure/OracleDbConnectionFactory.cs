using System;
using System.Data.Common;
using ExcelProcessor.Core.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace ExcelProcessor.Data.Infrastructure
{
	public sealed class OracleDbConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;
		public OracleDbConnectionFactory(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}
		public DbConnection CreateConnection()
		{
			return new OracleConnection(_connectionString);
		}
	}
} 