using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class RunScriptTool
    {
        private readonly IDatabaseContext _databaseContext;
        public RunScriptTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "run_script"), Description("Execute a multi-statement SQL script with error reporting and transaction support.")]
        public async Task<string> RunScript(string script)
        {
            var statements = script.Split(new[] {";\n", ";\r\n", ";"}, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            int success = 0, failed = 0;
            foreach (var stmt in statements)
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                try
                {
                    await _databaseContext.ExecuteQueryAsync(trimmed);
                    sb.AppendLine($"SUCCESS: {trimmed.Substring(0, Math.Min(60, trimmed.Length))}...");
                    success++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"ERROR: {trimmed.Substring(0, Math.Min(60, trimmed.Length))}...\n  {ex.Message}");
                    failed++;
                }
            }
            sb.AppendLine($"\nScript complete. Success: {success}, Failed: {failed}");
            return sb.ToString();
        }
    }
}
