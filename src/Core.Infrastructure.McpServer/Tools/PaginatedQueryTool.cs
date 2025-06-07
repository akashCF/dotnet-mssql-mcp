using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class PaginatedQueryTool
    {
        private readonly IDatabaseContext _databaseContext;
        public PaginatedQueryTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "paginated_query"), Description("Execute a paginated SQL query.")]
        public async Task<string> PaginatedQuery(string query, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            var offset = (page - 1) * pageSize;
            var paginatedQuery = $"{query} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            var sb = new StringBuilder();
            using var reader = await _databaseContext.ExecuteQueryAsync(paginatedQuery);
            int row = 0;
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append($"{reader.GetName(i)}: {await reader.GetFieldValueAsync<object>(i)}; ");
                }
                sb.AppendLine();
                row++;
            }
            return row > 0 ? sb.ToString() : "No results.";
        }
    }
}
