using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetViewDetailsTool
    {
        private readonly IDatabaseContext _databaseContext;
        public GetViewDetailsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "get_view_details"), Description("Get details for a view, including columns and definition.")]
        public async Task<string> GetViewDetails(string viewName)
        {
            var sql = @"SELECT TABLE_SCHEMA, TABLE_NAME, VIEW_DEFINITION FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_NAME = @viewName";
            var parameters = new[] { new Microsoft.Data.SqlClient.SqlParameter("@viewName", viewName) };
            var sb = new StringBuilder();
            using var reader = await _databaseContext.ExecuteQueryAsync(sql);
            while (await reader.ReadAsync())
            {
                sb.AppendLine($"Schema: {await reader.GetFieldValueAsync<string>(0)}, Name: {await reader.GetFieldValueAsync<string>(1)}\nDefinition:\n{await reader.GetFieldValueAsync<string>(2)}");
            }
            return sb.Length > 0 ? sb.ToString() : $"No view found with name {viewName}";
        }
    }
}
