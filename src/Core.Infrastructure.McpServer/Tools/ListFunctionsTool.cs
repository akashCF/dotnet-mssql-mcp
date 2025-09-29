using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListFunctionsTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListFunctionsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "list_functions"), Description("List all user-defined functions in the current database.")]
        public async Task<string> ListFunctions()
        {
            try
            {
                var sql = @"
                    SELECT 
                        ROUTINE_SCHEMA,
                        ROUTINE_NAME,
                        ROUTINE_TYPE,
                        DATA_TYPE,
                        IS_DETERMINISTIC,
                        CREATED,
                        LAST_ALTERED
                    FROM INFORMATION_SCHEMA.ROUTINES
                    WHERE ROUTINE_TYPE = 'FUNCTION'
                    ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

                var sb = new StringBuilder();
                sb.AppendLine("Available Functions:");
                sb.AppendLine();
                sb.AppendLine("Schema | Function Name                 | Type     | Return Type | Deterministic | Created             | Last Altered");
                sb.AppendLine("------ | ----------------------------- | -------- | ----------- | ------------- | ------------------- | -------------------");

                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool hasData = false;
                
                while (await reader.ReadAsync())
                {
                    hasData = true;
                    var schema = (await reader.GetFieldValueAsync<string>(0)).PadRight(6);
                    var functionName = (await reader.GetFieldValueAsync<string>(1)).PadRight(29);
                    var routineType = (await reader.GetFieldValueAsync<string>(2)).PadRight(8);
                    var dataType = (await reader.GetFieldValueAsync<string?>(3) ?? "N/A").PadRight(11);
                    var isDeterministic = (await reader.GetFieldValueAsync<string>(4)).PadRight(13);
                    var created = (await reader.GetFieldValueAsync<DateTime?>(5))?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A".PadRight(19);
                    var lastAltered = (await reader.GetFieldValueAsync<DateTime?>(6))?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                    
                    sb.AppendLine($"{schema} | {functionName} | {routineType} | {dataType} | {isDeterministic} | {created} | {lastAltered}");
                }
                
                return hasData ? sb.ToString() : "No functions found in the database.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing functions");
            }
        }
    }
}