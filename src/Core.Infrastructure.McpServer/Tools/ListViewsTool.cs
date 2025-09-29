using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListViewsTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListViewsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "list_views"), Description("List all views in the current database.")]
        public async Task<string> ListViews()
        {
            try
            {
                var sql = @"
                    SELECT 
                        TABLE_SCHEMA,
                        TABLE_NAME,
                        IS_UPDATABLE,
                        CHECK_OPTION
                    FROM INFORMATION_SCHEMA.VIEWS
                    ORDER BY TABLE_SCHEMA, TABLE_NAME";

                var sb = new StringBuilder();
                sb.AppendLine("Available Views:");
                sb.AppendLine();
                sb.AppendLine("Schema    | View Name                       | Updatable | Check Option");
                sb.AppendLine("--------- | ------------------------------- | --------- | ------------");

                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool hasData = false;
                
                while (await reader.ReadAsync())
                {
                    hasData = true;
                    var schema = (await reader.GetFieldValueAsync<string>(0)).PadRight(9);
                    var viewName = (await reader.GetFieldValueAsync<string>(1)).PadRight(31);
                    var updatable = (await reader.GetFieldValueAsync<string>(2)).PadRight(9);
                    var checkOption = await reader.GetFieldValueAsync<string?>(3) ?? "NONE";
                    
                    sb.AppendLine($"{schema} | {viewName} | {updatable} | {checkOption}");
                }
                
                return hasData ? sb.ToString() : "No views found in the database.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing views");
            }
        }
    }
}