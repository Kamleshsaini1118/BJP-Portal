using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Data
{
    public class DapperContext : IDapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("StockPortalDb")
                ?? throw new InvalidOperationException("Connection string 'StockPortalDb' not found in configuration.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
