using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTriggerDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetTriggerDefinitionTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_trigger_definition"), Description("Get the complete definition (CREATE statement) for a trigger.")]
        public async Task<string> GetTriggerDefinition(
            [Description("Name of the trigger")] string triggerName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        s.name AS schema_name,
                        t.name AS trigger_name,
                        o.name AS table_name,
                        m.definition,
                        t.is_disabled,
                        t.is_instead_of_trigger,
                        t.create_date,
                        t.modify_date,
                        CASE 
                            WHEN OBJECTPROPERTY(t.object_id, 'ExecIsInsertTrigger') = 1 THEN 'INSERT '
                            ELSE ''
                        END +
                        CASE 
                            WHEN OBJECTPROPERTY(t.object_id, 'ExecIsUpdateTrigger') = 1 THEN 'UPDATE '
                            ELSE ''
                        END +
                        CASE 
                            WHEN OBJECTPROPERTY(t.object_id, 'ExecIsDeleteTrigger') = 1 THEN 'DELETE '
                            ELSE ''
                        END AS trigger_events
                    FROM sys.triggers t
                    INNER JOIN sys.objects o ON t.parent_id = o.object_id
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    INNER JOIN sys.sql_modules m ON t.object_id = m.object_id
                    WHERE t.name = '{triggerName.Replace("'", "''")}' 
                    AND t.parent_class = 1
                    AND ({(schemaName == null ? "1=1" : $"s.name = '{schemaName.Replace("'", "''")}'")})";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;

                while (await reader.ReadAsync())
                {
                    found = true;
                    var schema = await reader.GetFieldValueAsync<string>(0);
                    var name = await reader.GetFieldValueAsync<string>(1);
                    var tableName = await reader.GetFieldValueAsync<string>(2);
                    var definition = await reader.GetFieldValueAsync<string>(3);
                    var isDisabled = await reader.GetFieldValueAsync<bool>(4);
                    var isInsteadOf = await reader.GetFieldValueAsync<bool>(5);
                    var created = await reader.GetFieldValueAsync<DateTime>(6);
                    var modified = await reader.GetFieldValueAsync<DateTime>(7);
                    var triggerEvents = (await reader.GetFieldValueAsync<string>(8)).Trim();

                    sb.AppendLine($"## Trigger: {schema}.{name}");
                    sb.AppendLine();
                    sb.AppendLine($"**Table:** {schema}.{tableName}");
                    sb.AppendLine($"**Events:** {triggerEvents}");
                    sb.AppendLine($"**Type:** {(isInsteadOf ? "INSTEAD OF" : "AFTER")}");
                    sb.AppendLine($"**Status:** {(isDisabled ? "DISABLED" : "ENABLED")}");
                    sb.AppendLine($"**Created:** {created:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"**Last Modified:** {modified:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("### Definition:");
                    sb.AppendLine("```sql");
                    sb.AppendLine(definition);
                    sb.AppendLine("```");
                }

                return found ? sb.ToString() : $"No trigger found with name '{triggerName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting trigger definition");
            }
        }
    }
}