using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetViewDefinitionTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetViewDefinitionTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_view_definition"), Description("Get the complete definition (CREATE statement) for a view.")]
        public async Task<string> GetViewDefinition(
            [Description("Name of the view")] string viewName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        TABLE_SCHEMA,
                        TABLE_NAME,
                        VIEW_DEFINITION,
                        IS_UPDATABLE,
                        CHECK_OPTION
                    FROM INFORMATION_SCHEMA.VIEWS 
                    WHERE TABLE_NAME = '{viewName.Replace("'", "''")}' 
                    AND ({(schemaName == null ? "1=1" : $"TABLE_SCHEMA = '{schemaName.Replace("'", "''")}'")})";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;

                while (await reader.ReadAsync())
                {
                    found = true;
                    var schema = await reader.GetFieldValueAsync<string>(0);
                    var name = await reader.GetFieldValueAsync<string>(1);
                    var definition = await reader.GetFieldValueAsync<string>(2);
                    var updatable = await reader.GetFieldValueAsync<string>(3);
                    var checkOption = await reader.GetFieldValueAsync<string?>(4);

                    sb.AppendLine($"## View: {schema}.{name}");
                    sb.AppendLine();
                    sb.AppendLine($"**Updatable:** {updatable}");
                    if (!string.IsNullOrEmpty(checkOption))
                        sb.AppendLine($"**Check Option:** {checkOption}");
                    sb.AppendLine();
                    sb.AppendLine("### Definition:");
                    sb.AppendLine("```sql");
                    sb.AppendLine($"CREATE VIEW [{schema}].[{name}] AS");
                    sb.AppendLine(definition);
                    sb.AppendLine("```");
                }

                return found ? sb.ToString() : $"No view found with name '{viewName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting view definition");
            }
        }
    }
}