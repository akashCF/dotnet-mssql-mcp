using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ImportTableCsvTool
    {
        private readonly IDatabaseContext _databaseContext;
        public ImportTableCsvTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "import_table_csv"), Description("Import data from CSV into a table.")]
        public async Task<string> ImportTableCsv(string tableName, string filePath)
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
                return "CSV must have a header and at least one data row.";

            var columns = lines[0].Split(',');
            var rowCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length != columns.Length) continue;
                var valueList = string.Join(",", values.Select(v => $"'" + v.Replace("'", "''") + "'"));
                var insertSql = $"INSERT INTO [{tableName}] ({string.Join(",", columns)}) VALUES ({valueList})";
                await _databaseContext.ExecuteQueryAsync(insertSql);
                rowCount++;
            }
            return $"Imported {rowCount} rows into {tableName}.";
        }
    }
}
