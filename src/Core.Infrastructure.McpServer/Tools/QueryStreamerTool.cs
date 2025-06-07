using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class QueryStreamerTool
    {
        private readonly IDatabaseContext _databaseContext;
        public QueryStreamerTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [McpServerTool(Name = "query_streamer"), Description("Stream large query results in batches.")]
        public async IAsyncEnumerable<string> QueryStreamer(string query, int batchSize = 100)
        {
            using var reader = await _databaseContext.ExecuteQueryAsync(query);
            var sb = new StringBuilder();
            int row = 0;
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append($"{reader.GetName(i)}: {await reader.GetFieldValueAsync<object>(i)}; ");
                }
                sb.AppendLine();
                row++;
                if (row % batchSize == 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
            if (sb.Length > 0)
                yield return sb.ToString();
        }
    }
}
