using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class DiscoverTool
    {
        private readonly IDatabaseContext _databaseContext;
        public DiscoverTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "discover"), Description("Discover all tables, views, functions, and procedures in the current database.")]
        public async Task<string> Discover()
        {
            var sb = new StringBuilder();
            // Tables
            sb.AppendLine("Tables:");
            var tableReader = await _databaseContext.ExecuteQueryAsync("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");
            while (await tableReader.ReadAsync())
                sb.AppendLine($"  {await tableReader.GetFieldValueAsync<string>(0)}.{await tableReader.GetFieldValueAsync<string>(1)}");
            tableReader.Dispose();
            // Views
            sb.AppendLine("\nViews:");
            var viewReader = await _databaseContext.ExecuteQueryAsync("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS");
            while (await viewReader.ReadAsync())
                sb.AppendLine($"  {await viewReader.GetFieldValueAsync<string>(0)}.{await viewReader.GetFieldValueAsync<string>(1)}");
            viewReader.Dispose();
            // Functions
            sb.AppendLine("\nFunctions:");
            var funcReader = await _databaseContext.ExecuteQueryAsync("SELECT ROUTINE_SCHEMA, ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'FUNCTION'");
            while (await funcReader.ReadAsync())
                sb.AppendLine($"  {await funcReader.GetFieldValueAsync<string>(0)}.{await funcReader.GetFieldValueAsync<string>(1)}");
            funcReader.Dispose();
            // Procedures
            sb.AppendLine("\nProcedures:");
            var procReader = await _databaseContext.ExecuteQueryAsync("SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'");
            while (await procReader.ReadAsync())
                sb.AppendLine($"  {await procReader.GetFieldValueAsync<string>(0)}.{await procReader.GetFieldValueAsync<string>(1)}");
            procReader.Dispose();
            return sb.ToString();
        }
    }
}
