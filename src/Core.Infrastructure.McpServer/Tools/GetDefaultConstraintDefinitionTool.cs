using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetDefaultConstraintDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetDefaultConstraintDefinitionTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_default_constraint_definition"), Description("Get default constraint definitions for a table or specific column.")]
        public async Task<string> GetDefaultConstraintDefinition(
            [Description("Name of the table")] string tableName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo",
            [Description("Column name (optional, if not provided returns all default constraints for the table)")] string? columnName = null)
        {
            try
            {
                var sql = $@"
                    SELECT 
                        s.name AS schema_name,
                        t.name AS table_name,
                        c.name AS column_name,
                        dc.name AS constraint_name,
                        dc.definition AS constraint_definition,
                        dc.create_date,
                        dc.modify_date,
                        dc.is_system_named
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN sys.columns c ON t.object_id = c.object_id
                    INNER JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
                    WHERE t.name = '{tableName.Replace("'", "''")}' 
                    AND ({(schemaName == null ? "1=1" : $"s.name = '{schemaName.Replace("'", "''")}'")})
                    AND ({(columnName == null ? "1=1" : $"c.name = '{columnName.Replace("'", "''")}'")})
                    ORDER BY c.column_id";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;
                int constraintCount = 0;

                if (!string.IsNullOrEmpty(columnName))
                {
                    sb.AppendLine($"## Default Constraints for Column: {schemaName ?? "dbo"}.{tableName}.{columnName}");
                }
                else
                {
                    sb.AppendLine($"## Default Constraints for Table: {schemaName ?? "dbo"}.{tableName}");
                }
                sb.AppendLine();

                while (await reader.ReadAsync())
                {
                    found = true;
                    constraintCount++;
                    
                    var schema = await reader.GetFieldValueAsync<string>(0);
                    var table = await reader.GetFieldValueAsync<string>(1);
                    var column = await reader.GetFieldValueAsync<string>(2);
                    var constraintName = await reader.GetFieldValueAsync<string>(3);
                    var definition = await reader.GetFieldValueAsync<string>(4);
                    var createDate = await reader.GetFieldValueAsync<DateTime>(5);
                    var modifyDate = await reader.GetFieldValueAsync<DateTime>(6);
                    var isSystemNamed = await reader.GetFieldValueAsync<bool>(7);

                    sb.AppendLine($"### {constraintCount}. {constraintName}");
                    sb.AppendLine($"**Column:** {column}");
                    sb.AppendLine($"**Definition:** `{definition}`");
                    sb.AppendLine($"**System Named:** {(isSystemNamed ? "Yes" : "No")}");
                    sb.AppendLine($"**Created:** {createDate:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"**Last Modified:** {modifyDate:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("**SQL to recreate:**");
                    sb.AppendLine("```sql");
                    if (isSystemNamed)
                    {
                        sb.AppendLine($"ALTER TABLE [{schema}].[{table}] ADD DEFAULT {definition} FOR [{column}]");
                    }
                    else
                    {
                        sb.AppendLine($"ALTER TABLE [{schema}].[{table}] ADD CONSTRAINT [{constraintName}] DEFAULT {definition} FOR [{column}]");
                    }
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (found)
                {
                    string target = !string.IsNullOrEmpty(columnName) ? $"column '{columnName}'" : $"table '{tableName}'";
                    sb.Insert(0, $"Found {constraintCount} default constraint{(constraintCount != 1 ? "s" : "")} for {target}.\n\n");
                    return sb.ToString();
                }
                else
                {
                    string target = !string.IsNullOrEmpty(columnName) 
                        ? $"column '{columnName}' in table '{tableName}'" 
                        : $"table '{tableName}'";
                    return $"No default constraints found for {target} in schema '{schemaName ?? "dbo"}'.";
                }
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting default constraint definitions");
            }
        }
    }
}