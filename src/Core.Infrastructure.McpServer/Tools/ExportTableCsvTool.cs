using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.IO;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ExportTableCsvTool
    {
        private readonly IDatabaseContext _databaseContext;
        public ExportTableCsvTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "export_table_csv"), Description("Export a table or query result to CSV.")]
        public async Task<string> ExportTableCsv(string query, string filePath)
        {
            using var reader = await _databaseContext.ExecuteQueryAsync(query);
            if (reader.FieldCount == 0)
                return "No columns to export.";

            var sb = new StringBuilder();
            // Write header
            for (int i = 0; i < reader.FieldCount; i++)
            {
                sb.Append(reader.GetName(i));
                if (i < reader.FieldCount - 1) sb.Append(",");
            }
            sb.AppendLine();

            // Write rows
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.GetFieldValueAsync<object>(i);
                    sb.Append(value?.ToString()?.Replace("\"", "\"\"") ?? "");
                    if (i < reader.FieldCount - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
            return $"Exported to {filePath}";
        }
    }
}
