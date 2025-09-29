using Core.Application.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Core.Infrastructure.McpServer.Extensions;
using System.Text;

namespace Core.Infrastructure.McpServer.Tools
{
    [McpServerToolType]
    public class ListRelationshipsTool
    {
        private readonly IDatabaseContext _databaseContext;

        public ListRelationshipsTool(IDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
        }

        [McpServerTool(Name = "list_relationships"), Description("List all foreign key relationships between tables in the database.")]
        public async Task<string> ListRelationships()
        {
            try
            {
                var sql = @"
                    SELECT 
                        fk.name AS foreign_key_name,
                        OBJECT_SCHEMA_NAME(fk.parent_object_id) AS parent_schema,
                        OBJECT_NAME(fk.parent_object_id) AS parent_table,
                        STRING_AGG(pc.name, ', ') AS parent_columns,
                        OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS referenced_schema,
                        OBJECT_NAME(fk.referenced_object_id) AS referenced_table,
                        STRING_AGG(rc.name, ', ') AS referenced_columns,
                        fk.delete_referential_action_desc AS delete_action,
                        fk.update_referential_action_desc AS update_action,
                        fk.is_disabled,
                        fk.is_not_trusted,
                        fk.create_date
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
                    INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
                    GROUP BY 
                        fk.name,
                        fk.parent_object_id,
                        fk.referenced_object_id,
                        fk.delete_referential_action_desc,
                        fk.update_referential_action_desc,
                        fk.is_disabled,
                        fk.is_not_trusted,
                        fk.create_date
                    ORDER BY 
                        OBJECT_SCHEMA_NAME(fk.parent_object_id),
                        OBJECT_NAME(fk.parent_object_id),
                        fk.name";

                var sb = new StringBuilder();
                sb.AppendLine("## Foreign Key Relationships");
                sb.AppendLine();

                using var reader = await _databaseContext.ExecuteQueryAsync(sql);
                bool hasData = false;
                int relationshipCount = 0;

                while (await reader.ReadAsync())
                {
                    hasData = true;
                    relationshipCount++;
                    
                    var fkName = await reader.GetFieldValueAsync<string>(0);
                    var parentSchema = await reader.GetFieldValueAsync<string>(1);
                    var parentTable = await reader.GetFieldValueAsync<string>(2);
                    var parentColumns = await reader.GetFieldValueAsync<string>(3);
                    var referencedSchema = await reader.GetFieldValueAsync<string>(4);
                    var referencedTable = await reader.GetFieldValueAsync<string>(5);
                    var referencedColumns = await reader.GetFieldValueAsync<string>(6);
                    var deleteAction = await reader.GetFieldValueAsync<string>(7);
                    var updateAction = await reader.GetFieldValueAsync<string>(8);
                    var isDisabled = await reader.GetFieldValueAsync<bool>(9);
                    var isNotTrusted = await reader.GetFieldValueAsync<bool>(10);
                    var createDate = await reader.GetFieldValueAsync<DateTime>(11);

                    sb.AppendLine($"### {relationshipCount}. {fkName}");
                    sb.AppendLine($"**Parent Table:** {parentSchema}.{parentTable} ({parentColumns})");
                    sb.AppendLine($"**Referenced Table:** {referencedSchema}.{referencedTable} ({referencedColumns})");
                    sb.AppendLine($"**Delete Action:** {deleteAction}");
                    sb.AppendLine($"**Update Action:** {updateAction}");
                    
                    var status = new List<string>();
                    if (isDisabled) status.Add("DISABLED");
                    if (isNotTrusted) status.Add("NOT TRUSTED");
                    if (!status.Any()) status.Add("ENABLED");
                    
                    sb.AppendLine($"**Status:** {string.Join(", ", status)}");
                    sb.AppendLine($"**Created:** {createDate:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                }

                if (hasData)
                {
                    sb.Insert(0, $"Found {relationshipCount} foreign key relationship{(relationshipCount != 1 ? "s" : "")} in the database.\n\n");
                    return sb.ToString();
                }
                else
                {
                    return "No foreign key relationships found in the database.";
                }
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("listing relationships");
            }
        }
    }
}