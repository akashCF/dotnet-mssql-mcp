using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListSchemasTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListSchemasTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "list_schemas"), Description("List all schemas in the current database.")]
        public async Task<string> ListSchemas()
        {
            try
            {
                var sql = @"
                    SELECT 
                        s.name AS schema_name,
                        p.name AS owner_name,
                        s.schema_id
                    FROM sys.schemas s
                    INNER JOIN sys.database_principals p ON s.principal_id = p.principal_id
                    ORDER BY s.name";

                var sb = new StringBuilder();
                sb.AppendLine("Available Schemas:");
                sb.AppendLine();
                sb.AppendLine("Schema Name        | Owner              | Schema ID");
                sb.AppendLine("------------------ | ------------------ | ---------");

                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool hasData = false;
                
                while (await reader.ReadAsync())
                {
                    hasData = true;
                    var schemaName = (await reader.GetFieldValueAsync<string>(0)).PadRight(18);
                    var ownerName = (await reader.GetFieldValueAsync<string>(1)).PadRight(18);
                    var schemaId = await reader.GetFieldValueAsync<int>(2);
                    
                    sb.AppendLine($"{schemaName} | {ownerName} | {schemaId}");
                }
                
                return hasData ? sb.ToString() : "No schemas found in the database.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing schemas");
            }
        }
    }
}