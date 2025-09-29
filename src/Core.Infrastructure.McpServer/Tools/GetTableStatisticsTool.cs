using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableStatisticsTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetTableStatisticsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_table_statistics"), Description("Get comprehensive statistics for a table including row count, size, and index usage.")]
        public async Task<string> GetTableStatistics(
            [Description("Name of the table")] string tableName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        s.name AS schema_name,
                        t.name AS table_name,
                        p.rows AS row_count,
                        SUM(a.total_pages) * 8 AS total_space_kb,
                        SUM(a.used_pages) * 8 AS used_space_kb,
                        (SUM(a.total_pages) - SUM(a.used_pages)) * 8 AS unused_space_kb,
                        SUM(a.data_pages) * 8 AS data_space_kb,
                        t.create_date,
                        t.modify_date,
                        CASE 
                            WHEN t.is_replicated = 1 THEN 'Yes' 
                            ELSE 'No' 
                        END AS is_replicated,
                        CASE 
                            WHEN t.has_replication_filter = 1 THEN 'Yes' 
                            ELSE 'No' 
                        END AS has_replication_filter
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN sys.partitions p ON t.object_id = p.object_id
                    INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                    WHERE t.name = '{tableName.Replace("'", "''")}' 
                    AND ({(schemaName == null ? "1=1" : $"s.name = '{schemaName.Replace("'", "''")}'")})
                    AND p.index_id IN (0,1)  -- Heap or clustered index
                    GROUP BY s.name, t.name, p.rows, t.create_date, t.modify_date, t.is_replicated, t.has_replication_filter";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;

                while (await reader.ReadAsync())
                {
                    found = true;
                    var schema = await reader.GetFieldValueAsync<string>(0);
                    var name = await reader.GetFieldValueAsync<string>(1);
                    var rowCount = await reader.GetFieldValueAsync<long>(2);
                    var totalSpaceKb = await reader.GetFieldValueAsync<long>(3);
                    var usedSpaceKb = await reader.GetFieldValueAsync<long>(4);
                    var unusedSpaceKb = await reader.GetFieldValueAsync<long>(5);
                    var dataSpaceKb = await reader.GetFieldValueAsync<long>(6);
                    var created = await reader.GetFieldValueAsync<DateTime>(7);
                    var modified = await reader.GetFieldValueAsync<DateTime>(8);
                    var isReplicated = await reader.GetFieldValueAsync<string>(9);
                    var hasReplicationFilter = await reader.GetFieldValueAsync<string>(10);

                    sb.AppendLine($"## Table Statistics: {schema}.{name}");
                    sb.AppendLine();
                    sb.AppendLine("### Basic Information");
                    sb.AppendLine($"**Row Count:** {rowCount:N0}");
                    sb.AppendLine($"**Created:** {created:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"**Last Modified:** {modified:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"**Replicated:** {isReplicated}");
                    sb.AppendLine($"**Has Replication Filter:** {hasReplicationFilter}");
                    sb.AppendLine();
                    
                    sb.AppendLine("### Storage Information");
                    sb.AppendLine($"**Total Space:** {totalSpaceKb:N0} KB ({totalSpaceKb / 1024.0:F2} MB)");
                    sb.AppendLine($"**Used Space:** {usedSpaceKb:N0} KB ({usedSpaceKb / 1024.0:F2} MB)");
                    sb.AppendLine($"**Unused Space:** {unusedSpaceKb:N0} KB ({unusedSpaceKb / 1024.0:F2} MB)");
                    sb.AppendLine($"**Data Space:** {dataSpaceKb:N0} KB ({dataSpaceKb / 1024.0:F2} MB)");
                    sb.AppendLine($"**Index Space:** {(usedSpaceKb - dataSpaceKb):N0} KB ({(usedSpaceKb - dataSpaceKb) / 1024.0:F2} MB)");
                }

                // Get additional statistics
                if (found)
                {
                    var indexStatsSql = $@"
                        SELECT 
                            COUNT(*) as index_count,
                            SUM(CASE WHEN is_unique = 1 THEN 1 ELSE 0 END) as unique_indexes,
                            SUM(CASE WHEN is_primary_key = 1 THEN 1 ELSE 0 END) as primary_keys
                        FROM sys.indexes i
                        INNER JOIN sys.objects o ON i.object_id = o.object_id
                        INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                        WHERE o.name = '{tableName.Replace("'", "''")}' 
                        AND ({(schemaName == null ? "1=1" : $"s.name = '{schemaName.Replace("'", "''")}'")})
                        AND i.type > 0";  // Exclude heap

                    using var indexReader = await _databaseContext.ExecuteQueryAsync(indexStatsSql);
                    if (await indexReader.ReadAsync())
                    {
                        var indexCount = await indexReader.GetFieldValueAsync<int>(0);
                        var uniqueIndexes = await indexReader.GetFieldValueAsync<int>(1);
                        var primaryKeys = await indexReader.GetFieldValueAsync<int>(2);

                        sb.AppendLine();
                        sb.AppendLine("### Index Information");
                        sb.AppendLine($"**Total Indexes:** {indexCount}");
                        sb.AppendLine($"**Unique Indexes:** {uniqueIndexes}");
                        sb.AppendLine($"**Primary Keys:** {primaryKeys}");
                    }
                }

                return found ? sb.ToString() : $"No statistics found for table '{tableName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table statistics");
            }
        }
    }
}