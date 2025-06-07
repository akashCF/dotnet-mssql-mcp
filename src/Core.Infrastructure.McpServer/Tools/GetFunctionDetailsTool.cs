using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetFunctionDetailsTool
    {
        private readonly IDatabaseContext _databaseContext;
        public GetFunctionDetailsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "get_function_details"), Description("Get details for a scalar or table-valued function.")]
        public async Task<string> GetFunctionDetails(string functionName)
        {
            var sql = @"SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                        FROM INFORMATION_SCHEMA.ROUTINES
                        WHERE ROUTINE_TYPE IN ('FUNCTION') AND ROUTINE_NAME = @functionName";
            var parameters = new[] { new Microsoft.Data.SqlClient.SqlParameter("@functionName", functionName) };
            var sb = new StringBuilder();
            using var reader = await _databaseContext.ExecuteQueryAsync(sql);
            while (await reader.ReadAsync())
            {
                sb.AppendLine($"Schema: {await reader.GetFieldValueAsync<string>(0)}, Name: {await reader.GetFieldValueAsync<string>(1)}, Type: {await reader.GetFieldValueAsync<string>(2)}, DataType: {await reader.GetFieldValueAsync<string>(3)}, MaxLength: {await reader.GetFieldValueAsync<object>(4)}");
            }
            return sb.Length > 0 ? sb.ToString() : $"No function found with name {functionName}";
        }
    }
}
