using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetIndexDetailsTool
    {
        private readonly IDatabaseContext _databaseContext;
        public GetIndexDetailsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "get_index_details"), Description("Get details for indexes on a table.")]
        public async Task<string> GetIndexDetails(string tableName)
        {
            var sql = @"SELECT ind.name AS IndexName, col.name AS ColumnName, ind.type_desc AS IndexType, ind.is_primary_key, ind.is_unique
                        FROM sys.indexes ind
                        INNER JOIN sys.index_columns ic ON ind.object_id = ic.object_id AND ind.index_id = ic.index_id
                        INNER JOIN sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id
                        INNER JOIN sys.tables t ON ind.object_id = t.object_id
                        WHERE t.name = @tableName";
            var parameters = new[] { new Microsoft.Data.SqlClient.SqlParameter("@tableName", tableName) };
            var sb = new StringBuilder();
            using var reader = await _databaseContext.ExecuteQueryAsync(sql);
            while (await reader.ReadAsync())
            {
                sb.AppendLine($"Index: {await reader.GetFieldValueAsync<string>(0)}, Column: {await reader.GetFieldValueAsync<string>(1)}, Type: {await reader.GetFieldValueAsync<string>(2)}, PrimaryKey: {await reader.GetFieldValueAsync<bool>(3)}, Unique: {await reader.GetFieldValueAsync<bool>(4)}");
            }
            return sb.Length > 0 ? sb.ToString() : $"No indexes found for table {tableName}";
        }
    }
}
