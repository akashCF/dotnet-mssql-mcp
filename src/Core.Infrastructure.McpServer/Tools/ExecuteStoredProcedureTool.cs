using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text.Json;
using System.Collections.Generic;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ExecuteStoredProcedureTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ExecuteStoredProcedureTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            Console.Error.WriteLine("ExecuteStoredProcedureTool constructed with database context service");
        }

        [McpServerTool(Name = "execute_stored_procedure"), 
         Description(@"Execute a stored procedure on the connected SQL Server database.

Parameters should be provided as a JSON object with parameter names as keys.
Both '@ParameterName' and 'ParameterName' formats are accepted.

Examples:
- Simple parameters: {""CustomerID"": 123, ""OrderDate"": ""2024-01-01""}
- With @ prefix: {""@CustomerID"": 123, ""@OrderDate"": ""2024-01-01""}
- Mixed types: {""ID"": 123, ""Name"": ""Test"", ""IsActive"": true, ""Price"": 99.99}
- Null values: {""CustomerID"": 123, ""Notes"": null}

The tool will automatically convert JSON values to the appropriate SQL types based on the stored procedure's parameter definitions.
Use 'get_stored_procedure_parameters' tool first to see what parameters are expected.")]
        public async Task<string> ExecuteStoredProcedure(string procedureName, string parameters)
        {
            Console.Error.WriteLine($"ExecuteStoredProcedure called with stored procedure: {procedureName}");
            
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }

            try
            {
                // Parse the parameters from JSON
                Dictionary<string, object?> paramDict;
                try
                {
                    paramDict = !string.IsNullOrWhiteSpace(parameters) 
                        ? JsonSerializer.Deserialize<Dictionary<string, object?>>(parameters, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        : new Dictionary<string, object?>();
                    
                    if (paramDict == null)
                    {
                        paramDict = new Dictionary<string, object?>();
                    }
                }
                catch (JsonException ex)
                {
                    return $"Error parsing parameters: {ex.Message}. Parameters must be a valid JSON object with parameter names as keys.";
                }
                
                // Use the DatabaseContext service to execute the stored procedure
                IAsyncDataReader reader = await _databaseContext.ExecuteStoredProcedureAsync(procedureName, paramDict);
                
                // Format results into a readable table
                return await reader.ToToolResult();
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("executing stored procedure");
            }
        }
    }
}