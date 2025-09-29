using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class GetTableConstraintsTool
    {
        private readonly IDatabaseContext _databaseContext;

        public GetTableConstraintsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "get_table_constraints"), Description("Get all constraints for a specific table including primary key, foreign keys, check constraints, and unique constraints.")]
        public async Task<string> GetTableConstraints(
            [Description("Name of the table")] string tableName,
            [Description("Schema name (optional, defaults to dbo)")] string? schemaName = "dbo")
        {
            try
            {
                var sql = $@"
                    SELECT 
                        tc.CONSTRAINT_NAME,
                        tc.CONSTRAINT_TYPE,
                        tc.TABLE_SCHEMA,
                        tc.TABLE_NAME,
                        kcu.COLUMN_NAME,
                        rc.UNIQUE_CONSTRAINT_NAME,
                        kcu2.TABLE_SCHEMA AS REFERENCED_TABLE_SCHEMA,
                        kcu2.TABLE_NAME AS REFERENCED_TABLE_NAME,
                        kcu2.COLUMN_NAME AS REFERENCED_COLUMN_NAME,
                        cc.CHECK_CLAUSE
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
                        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME 
                        AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                        AND tc.TABLE_NAME = kcu.TABLE_NAME
                    LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc 
                        ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME 
                        AND tc.TABLE_SCHEMA = rc.CONSTRAINT_SCHEMA
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu2 
                        ON rc.UNIQUE_CONSTRAINT_NAME = kcu2.CONSTRAINT_NAME
                        AND rc.UNIQUE_CONSTRAINT_SCHEMA = kcu2.CONSTRAINT_SCHEMA
                    LEFT JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc 
                        ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = cc.CONSTRAINT_SCHEMA
                    WHERE tc.TABLE_NAME = '{tableName.Replace("'", "''")}' 
                    AND ({(schemaName == null ? "1=1" : $"tc.TABLE_SCHEMA = '{schemaName.Replace("'", "''")}'")})
                    ORDER BY tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION";

                var sb = new StringBuilder();
                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool found = false;
                string currentConstraint = "";
                string currentType = "";
                var columns = new List<string>();

                sb.AppendLine($"## Constraints for Table: {schemaName ?? "dbo"}.{tableName}");
                sb.AppendLine();

                while (await reader.ReadAsync())
                {
                    found = true;
                    var constraintName = await reader.GetFieldValueAsync<string>(0);
                    var constraintType = await reader.GetFieldValueAsync<string>(1);
                    var tableSchema = await reader.GetFieldValueAsync<string>(2);
                    var tableNameResult = await reader.GetFieldValueAsync<string>(3);
                    var columnName = await reader.GetFieldValueAsync<string?>(4);
                    var uniqueConstraintName = await reader.GetFieldValueAsync<string?>(5);
                    var refTableSchema = await reader.GetFieldValueAsync<string?>(6);
                    var refTableName = await reader.GetFieldValueAsync<string?>(7);
                    var refColumnName = await reader.GetFieldValueAsync<string?>(8);
                    var checkClause = await reader.GetFieldValueAsync<string?>(9);

                    if (currentConstraint != constraintName)
                    {
                        // Output previous constraint if exists
                        if (!string.IsNullOrEmpty(currentConstraint))
                        {
                            OutputConstraint(sb, currentConstraint, currentType, columns, refTableSchema, refTableName, refColumnName, checkClause);
                            columns.Clear();
                        }

                        currentConstraint = constraintName;
                        currentType = constraintType;
                    }

                    if (!string.IsNullOrEmpty(columnName))
                        columns.Add(columnName);
                }

                // Output the last constraint
                if (!string.IsNullOrEmpty(currentConstraint))
                {
                    var lastRefTableSchema = "";
                    var lastRefTableName = "";
                    var lastRefColumnName = "";
                    var lastCheckClause = "";
                    
                    // Need to get these values for the last constraint
                    using var reader2 = await _databaseContext.ExecuteQueryAsync(sql);
                    while (await reader2.ReadAsync())
                    {
                        var constraintName = await reader2.GetFieldValueAsync<string>(0);
                        if (constraintName == currentConstraint)
                        {
                            lastRefTableSchema = await reader2.GetFieldValueAsync<string?>(6);
                            lastRefTableName = await reader2.GetFieldValueAsync<string?>(7);
                            lastRefColumnName = await reader2.GetFieldValueAsync<string?>(8);
                            lastCheckClause = await reader2.GetFieldValueAsync<string?>(9);
                            break;
                        }
                    }
                    
                    OutputConstraint(sb, currentConstraint, currentType, columns, lastRefTableSchema, lastRefTableName, lastRefColumnName, lastCheckClause);
                }

                return found ? sb.ToString() : $"No constraints found for table '{tableName}' in schema '{schemaName ?? "dbo"}'.";
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting table constraints");
            }
        }

        private void OutputConstraint(StringBuilder sb, string constraintName, string constraintType, List<string> columns, string? refTableSchema, string? refTableName, string? refColumnName, string? checkClause)
        {
            sb.AppendLine($"### {constraintType}: {constraintName}");
            
            if (columns.Any())
                sb.AppendLine($"**Columns:** {string.Join(", ", columns)}");
            
            if (constraintType == "FOREIGN KEY" && !string.IsNullOrEmpty(refTableName))
            {
                sb.AppendLine($"**References:** {refTableSchema}.{refTableName}({refColumnName})");
            }
            
            if (constraintType == "CHECK" && !string.IsNullOrEmpty(checkClause))
            {
                sb.AppendLine($"**Check Clause:** {checkClause}");
            }
            
            sb.AppendLine();
        }
    }
}