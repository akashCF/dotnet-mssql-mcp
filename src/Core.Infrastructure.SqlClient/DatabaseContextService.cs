using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Interfaces;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Implementation of the IDatabaseContext interface for SQL Server databases.
    /// Provides operations for working with tables in the context of a specific database.
    /// </summary>
    public class DatabaseContextService : IDatabaseContext
    {
        private readonly IDatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the DatabaseContextService class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        public DatabaseContextService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) 
                throw new ArgumentNullException(nameof(connectionString));
            
            var capabilityDetector = new SqlServerCapabilityDetector(connectionString);
            _databaseService = new DatabaseService(connectionString, capabilityDetector);
        }

        /// <summary>
        /// Initializes a new instance of the DatabaseContextService class with an existing database service.
        /// </summary>
        /// <param name="databaseService">The database service to use</param>
        public DatabaseContextService(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Lists all tables in the current database context.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        public async Task<IEnumerable<TableInfo>> ListTablesAsync(CancellationToken cancellationToken = default)
        {
            // Call the database service without specifying a database name to use the current context
            return await _databaseService.ListTablesAsync(databaseName: null, cancellationToken);
        }

        /// <summary>
        /// Gets the schema information for a specific table in the current database context.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        public async Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            // Call the database service without specifying a database name to use the current context
            return await _databaseService.GetTableSchemaAsync(tableName, null, cancellationToken);
        }
        
        /// <summary>
        /// Executes a SQL query in the current database context.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        public async Task<IAsyncDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));
                
            // Call the database service without specifying a database name to use the current context
            return await _databaseService.ExecuteQueryAsync(query, null, cancellationToken);
        }
        
        /// <summary>
        /// Lists all stored procedures in the current database context.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        public async Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(CancellationToken cancellationToken = default)
        {
            // Call the database service without specifying a database name to use the current context
            return await _databaseService.ListStoredProceduresAsync(databaseName: null, cancellationToken);
        }

        /// <summary>
        /// Gets the definition information for a specific stored procedure in the current database context.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as SQL string</returns>
        public async Task<string> GetStoredProcedureDefinitionAsync(string procedureName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

            // Call the database service without specifying a database name to use the current context
            return await _databaseService.GetStoredProcedureDefinitionAsync(procedureName, null, cancellationToken);
        }
        
        /// <summary>
        /// Executes a stored procedure in the current database context.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        public async Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
                
            // Call the database service without specifying a database name to use the current context
            return await _databaseService.ExecuteStoredProcedureAsync(procedureName, parameters, null, cancellationToken);
        }
    }
}