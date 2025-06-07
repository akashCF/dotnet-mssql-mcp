using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class CursorGuideTool
    {
        private readonly IDatabaseContext _databaseContext;
        public CursorGuideTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "cursor_guide"), Description("List open cursors and provide cursor usage guidance.")]
        public async Task<string> CursorGuide()
        {
            var sql = @"SELECT * FROM sys.dm_exec_cursors(0)";
            var sb = new StringBuilder();
            using var reader = await _databaseContext.ExecuteQueryAsync(sql);
            while (await reader.ReadAsync())
            {
                sb.AppendLine($"Cursor: {await reader.GetFieldValueAsync<string>(reader.GetOrdinal("name"))}, Status: {await reader.GetFieldValueAsync<object>(reader.GetOrdinal("cursor_status"))}, Type: {await reader.GetFieldValueAsync<object>(reader.GetOrdinal("cursor_type"))}");
            }
            if (sb.Length == 0)
                sb.AppendLine("No open cursors found.");
            sb.AppendLine("\nTip: Use cursors sparingly. Prefer set-based operations for performance.");
            return sb.ToString();
        }
    }
}
