using Core.Application.Models;
using Core.Application.Interfaces;
using System.Collections.Generic;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for database context operations.
    /// Provides access to tables and queries in the currently connected database.
    /// </summary>
    public interface IDatabaseContext
    {
        /// <summary>
        /// Lists all tables in the current database.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of table information</returns>
        Task<IEnumerable<TableInfo>> ListTablesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema information for a specific table in the current database.
        /// </summary>
        /// <param name="tableName">The name of the table to get schema for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Table schema information</returns>
        Task<TableSchemaInfo> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a SQL query in the current database.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the query</returns>
        Task<IAsyncDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all stored procedures in the current database.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A collection of stored procedure information</returns>
        Task<IEnumerable<StoredProcedureInfo>> ListStoredProceduresAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the definition information for a specific stored procedure in the current database.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Stored procedure definition information</returns>
        Task<string> GetStoredProcedureDefinitionAsync(string procedureName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a stored procedure in the current database.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>An IAsyncDataReader with the results of the stored procedure</returns>
        Task<IAsyncDataReader> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    }
}