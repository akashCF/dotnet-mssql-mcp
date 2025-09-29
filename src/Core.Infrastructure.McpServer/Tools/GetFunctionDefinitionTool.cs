using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetFunctionDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetFunctionDefinitionTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_function_definition"), Description("Get the complete definition (CREATE statement) for a user-defined function.")]
        public async Task<string> GetFunctionDefinition(
            [Description("Name of the function")] string functionName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        ROUTINE_SCHEMA,
                        ROUTINE_NAME,
                        ROUTINE_DEFINITION,
                        DATA_TYPE,
                        IS_DETERMINISTIC,
                        ROUTINE_BODY,
                        CREATED,
                        LAST_ALTERED
                    FROM INFORMATION_SCHEMA.ROUTINES 
                    WHERE ROUTINE_NAME = '{functionName.Replace("'", "''")}' 
                    AND ROUTINE_TYPE = 'FUNCTION'
                    AND ({(schemaName == null ? "1=1" : $"ROUTINE_SCHEMA = '{schemaName.Replace("'", "''")}'")})";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;

                while (await reader.ReadAsync())
                {
                    found = true;
                    var schema = await reader.GetFieldValueAsync<string>(0);
                    var name = await reader.GetFieldValueAsync<string>(1);
                    var definition = await reader.GetFieldValueAsync<string>(2);
                    var dataType = await reader.GetFieldValueAsync<string?>(3);
                    var isDeterministic = await reader.GetFieldValueAsync<string>(4);
                    var routineBody = await reader.GetFieldValueAsync<string>(5);
                    var created = await reader.GetFieldValueAsync<DateTime?>(6);
                    var lastAltered = await reader.GetFieldValueAsync<DateTime?>(7);

                    sb.AppendLine($"## Function: {schema}.{name}");
                    sb.AppendLine();
                    sb.AppendLine($"**Return Type:** {dataType ?? "N/A"}");
                    sb.AppendLine($"**Deterministic:** {isDeterministic}");
                    sb.AppendLine($"**Language:** {routineBody}");
                    if (created.HasValue)
                        sb.AppendLine($"**Created:** {created.Value:yyyy-MM-dd HH:mm:ss}");
                    if (lastAltered.HasValue)
                        sb.AppendLine($"**Last Modified:** {lastAltered.Value:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("### Definition:");
                    sb.AppendLine("```sql");
                    sb.AppendLine(definition);
                    sb.AppendLine("```");
                }

                return found ? sb.ToString() : $"No function found with name '{functionName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting function definition");
            }
        }
    }
}