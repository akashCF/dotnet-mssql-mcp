using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListTriggersTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListTriggersTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "list_triggers"), Description("List all triggers in the current database.")]
        public async Task<string> ListTriggers()
        {
            try
            {
                var sql = @"
                    SELECT 
                        s.name AS schema_name,
                        t.name AS trigger_name,
                        o.name AS table_name,
                        t.is_disabled,
                        t.is_instead_of_trigger,
                        t.create_date,
                        t.modify_date
                    FROM sys.triggers t
                    INNER JOIN sys.objects o ON t.parent_id = o.object_id
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    WHERE t.parent_class = 1  -- Object or column triggers
                    ORDER BY s.name, o.name, t.name";

                var sb = new StringBuilder();
                sb.AppendLine("Available Triggers:");
                sb.AppendLine();
                sb.AppendLine("Schema | Table Name                    | Trigger Name                  | Disabled | Instead Of | Created             | Modified");
                sb.AppendLine("------ | ----------------------------- | ----------------------------- | -------- | ---------- | ------------------- | -------------------");

                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool hasData = false;
                
                while (await reader.ReadAsync())
                {
                    hasData = true;
                    var schema = (await reader.GetFieldValueAsync<string>(0)).PadRight(6);
                    var tableName = (await reader.GetFieldValueAsync<string>(2)).PadRight(29);
                    var triggerName = (await reader.GetFieldValueAsync<string>(1)).PadRight(29);
                    var isDisabled = (await reader.GetFieldValueAsync<bool>(3) ? "Yes" : "No").PadRight(8);
                    var isInsteadOf = (await reader.GetFieldValueAsync<bool>(4) ? "Yes" : "No").PadRight(10);
                    var created = (await reader.GetFieldValueAsync<DateTime>(5)).ToString("yyyy-MM-dd HH:mm:ss").PadRight(19);
                    var modified = (await reader.GetFieldValueAsync<DateTime>(6)).ToString("yyyy-MM-dd HH:mm:ss");
                    
                    sb.AppendLine($"{schema} | {tableName} | {triggerName} | {isDisabled} | {isInsteadOf} | {created} | {modified}");
                }
                
                return hasData ? sb.ToString() : "No triggers found in the database.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing triggers");
            }
        }
    }
}