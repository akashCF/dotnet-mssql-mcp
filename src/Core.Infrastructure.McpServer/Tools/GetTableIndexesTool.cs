using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableIndexesTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetTableIndexesTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_table_indexes"), Description("Get all indexes for a specific table including clustered, non-clustered, and unique indexes.")]
        public async Task<string> GetTableIndexes(
            [Description("Name of the table")] string tableName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        i.name AS index_name,
                        i.type_desc AS index_type,
                        i.is_unique,
                        i.is_primary_key,
                        i.is_unique_constraint,
                        i.fill_factor,
                        ic.key_ordinal,
                        c.name AS column_name,
                        ic.is_descending_key,
                        ic.is_included_column,
                        s.name AS schema_name,
                        t.name AS table_name
                    FROM sys.indexes i
                    INNER JOIN sys.objects t ON i.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    LEFT JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    LEFT JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE t.name = '{tableName.Replace("'", "''")}' 
                    AND t.type = 'U'  -- User tables only
                    AND ({(schemaName == null ? "1=1" : $"s.name = '{schemaName.Replace("'", "''")}'")})
                    AND i.type > 0  -- Exclude heap
                    ORDER BY i.name, ic.key_ordinal, ic.index_column_id";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;
                string currentIndex = "";
                var keyColumns = new List<string>();
                var includedColumns = new List<string>();
                string indexType = "";
                bool isUnique = false;
                bool isPrimaryKey = false;
                bool isUniqueConstraint = false;
                int fillFactor = 0;

                sb.AppendLine($"## Indexes for Table: {schemaName ?? "dbo"}.{tableName}");
                sb.AppendLine();

                while (await reader.ReadAsync())
                {
                    found = true;
                    var indexName = await reader.GetFieldValueAsync<string>(0);
                    var indexTypeDesc = await reader.GetFieldValueAsync<string>(1);
                    var unique = await reader.GetFieldValueAsync<bool>(2);
                    var primaryKey = await reader.GetFieldValueAsync<bool>(3);
                    var uniqueConstraint = await reader.GetFieldValueAsync<bool>(4);
                    var fillFactorValue = await reader.GetFieldValueAsync<byte>(5);
                    var keyOrdinal = await reader.GetFieldValueAsync<byte?>(6);
                    var columnName = await reader.GetFieldValueAsync<string?>(7);
                    var isDescending = await reader.GetFieldValueAsync<bool?>(8);
                    var isIncluded = await reader.GetFieldValueAsync<bool?>(9);

                    if (currentIndex != indexName)
                    {
                        // Output previous index if exists
                        if (!string.IsNullOrEmpty(currentIndex))
                        {
                            OutputIndex(sb, currentIndex, indexType, isUnique, isPrimaryKey, isUniqueConstraint, fillFactor, keyColumns, includedColumns);
                            keyColumns.Clear();
                            includedColumns.Clear();
                        }

                        currentIndex = indexName;
                        indexType = indexTypeDesc;
                        isUnique = unique;
                        isPrimaryKey = primaryKey;
                        isUniqueConstraint = uniqueConstraint;
                        fillFactor = fillFactorValue;
                    }

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        var columnDesc = columnName;
                        if (isDescending == true)
                            columnDesc += " DESC";
                        
                        if (isIncluded == true)
                            includedColumns.Add(columnDesc);
                        else if (keyOrdinal > 0)
                            keyColumns.Add(columnDesc);
                    }
                }

                // Output the last index
                if (!string.IsNullOrEmpty(currentIndex))
                {
                    OutputIndex(sb, currentIndex, indexType, isUnique, isPrimaryKey, isUniqueConstraint, fillFactor, keyColumns, includedColumns);
                }

                return found ? sb.ToString() : $"No indexes found for table '{tableName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table indexes");
            }
        }

        private void OutputIndex(StringBuilder sb, string indexName, string indexType, bool isUnique, bool isPrimaryKey, bool isUniqueConstraint, int fillFactor, List<string> keyColumns, List<string> includedColumns)
        {
            sb.AppendLine($"### {indexName}");
            sb.AppendLine($"**Type:** {indexType}");
            
            var properties = new List<string>();
            if (isPrimaryKey) properties.Add("Primary Key");
            if (isUniqueConstraint) properties.Add("Unique Constraint");
            else if (isUnique) properties.Add("Unique");
            
            if (properties.Any())
                sb.AppendLine($"**Properties:** {string.Join(", ", properties)}");
            
            if (keyColumns.Any())
                sb.AppendLine($"**Key Columns:** {string.Join(", ", keyColumns)}");
            
            if (includedColumns.Any())
                sb.AppendLine($"**Included Columns:** {string.Join(", ", includedColumns)}");
            
            if (fillFactor > 0)
                sb.AppendLine($"**Fill Factor:** {fillFactor}%");
            
            sb.AppendLine();
        }
    }
}