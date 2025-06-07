using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace Core.Infrastructure.SqlClient.Interfaces
{
    /// <summary>
    /// Interface for core database operations with context switching capability.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Lists all tables in the database with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(string? databaseName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of database information</returns>
        Task<IEnumerable<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table with optional database context switching.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, string? databaseName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a database exists and is accessible.
        /// </summary>
        /// <param name="databaseName">Name of the database to check</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the database exists and is accessible, otherwise false</returns>
        Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current database name from the connection string.
        /// </summary>
        /// <returns>The current database name</returns>
        string GetCurrentDatabaseName();
        
        /// <summary>
        /// Executes a SQL query with optional database context switching.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryAsync(string query, string? databaseName = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all stored procedures with optional database context switching.
        /// </summary>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(string? databaseName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the definition information for a specific stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition as SQL string</returns>
        Task<string> GetStoredProcedureDefinitionAsync(string procedureName, string? databaseName = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a stored procedure with optional database context switching.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="databaseName">Optional database name to switch context. If null, uses current database.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, string? databaseName = null, CancellationToken cancellationToken = default);
    }
}