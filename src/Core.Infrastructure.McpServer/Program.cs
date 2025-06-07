using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Configuration;
using Core.Infrastructure.McpServer.Tools;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Core.Infrastructure.McpServer
{
    internal class Program
    {
        /// <summary>
        /// Gets the version from the assembly's informational version attribute,
        /// which is set from the Version property in the project file.
        /// </summary>
        /// <returns>The version string, or "0.0.0" if not available</returns>
        private static string GetServerVersion()
        {
            return Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0";
        }
        
        /// <summary>
        /// Determines if the connection should use Server mode by examining if a specific database 
        /// is specified in the connection string.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>True if no database is specified (Server mode), false if a specific database is targeted (Database mode)</returns>
        public static bool IsServerMode(string connectionString)
        {
            try
            {
                string databaseName = null;
                
                // First try using SqlConnectionStringBuilder
                var builder = new SqlConnectionStringBuilder(connectionString);
                databaseName = builder.InitialCatalog;

                // If builder didn't get database, try direct parsing of Database=
                if (string.IsNullOrEmpty(databaseName) && connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse the Database parameter directly if SqlConnectionStringBuilder didn't work
                    var dbParamStart = connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
                    if (dbParamStart >= 0)
                    {
                        dbParamStart += "Database=".Length;
                        var dbParamEnd = connectionString.IndexOf(';', dbParamStart);
                        if (dbParamEnd < 0)
                            dbParamEnd = connectionString.Length;

                        databaseName = connectionString.Substring(dbParamStart, dbParamEnd - dbParamStart);
                    }
                }

                // Also check for Initial Catalog which is an alternative to Database
                if (string.IsNullOrEmpty(databaseName) && connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    var dbParamStart = connectionString.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
                    if (dbParamStart >= 0)
                    {
                        dbParamStart += "Initial Catalog=".Length;
                        var dbParamEnd = connectionString.IndexOf(';', dbParamStart);
                        if (dbParamEnd < 0)
                            dbParamEnd = connectionString.Length;

                        databaseName = connectionString.Substring(dbParamStart, dbParamEnd - dbParamStart);
                    }
                }

                Console.Error.WriteLine($"Database name from connection string: {databaseName ?? "(not specified)"}");
                
                // In Server mode if no database is specified or the database is empty
                return string.IsNullOrWhiteSpace(databaseName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking database name in connection string: {ex.Message}");
                return false; // Default to Database mode if there's an error
            }
        }
        
        static async Task Main(string[] args)
        {
            Console.Error.WriteLine("Starting MCP MSSQLClient Server...");
            var builder = Host.CreateApplicationBuilder(args);

            // Add appsettings.json configuration, use full path in case working folder is different
            string? basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            basePath ??= Directory.GetCurrentDirectory();

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, "appsettings.json"), 
                optional: true, 
                reloadOnChange: true);

            builder.Configuration.AddJsonFile(
                Path.Combine(basePath, $"appsettings.{builder.Environment.EnvironmentName}.json"), 
                optional: true, 
                reloadOnChange: true);

            builder.Configuration.AddUserSecrets(
                Assembly.GetExecutingAssembly(),
                optional: true,
                reloadOnChange: true);

            builder.Configuration.AddEnvironmentVariables();

            // Configure logging
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            
            // Get connection string from config 
            string? connectionString = builder.Configuration.GetValue<string>("MSSQL_CONNECTIONSTRING");
            if(connectionString == null)
            {
                Console.Error.WriteLine("MSSQL_CONNECTIONSTRING is not set in appsettings.json or environment variables.");
                return;
            }

            // Check if the connection string doesn't specify a database (Server mode)
            bool isServerMode = IsServerMode(connectionString);
            Console.Error.WriteLine($"Using server mode: {isServerMode}");

            // Register the database configuration
            var dbConfig = new DatabaseConfiguration 
            { 
                ConnectionString = connectionString,
                EnableExecuteQuery = builder.Configuration.GetValue<bool>("EnableExecuteQuery", false), // Default to false if not specified
                EnableExecuteStoredProcedure = builder.Configuration.GetValue<bool>("EnableExecuteStoredProcedure", false) // Default to false if not specified
            };
            Console.Error.WriteLine($"EnableExecuteQuery setting: {dbConfig.EnableExecuteQuery}");
            Console.Error.WriteLine($"EnableExecuteStoredProcedure setting: {dbConfig.EnableExecuteStoredProcedure}");
            builder.Services.AddSingleton(dbConfig);

            // Register our database services
            
            // Register capability detector first
            builder.Services.AddSingleton<ISqlServerCapabilityDetector>(provider => 
                new SqlServerCapabilityDetector(connectionString));
            
            // Then register the core database service with the capability detector
            builder.Services.AddSingleton<IDatabaseService>(provider => 
                new DatabaseService(connectionString, provider.GetRequiredService<ISqlServerCapabilityDetector>()));
            
            if (isServerMode)
            {
                // When in server mode, we need both database context and server database services
                builder.Services.AddSingleton<IDatabaseContext>(provider => 
                    new DatabaseContextService(provider.GetRequiredService<IDatabaseService>()));
                
                builder.Services.AddSingleton<IServerDatabase>(provider => 
                    new ServerDatabaseService(provider.GetRequiredService<IDatabaseService>()));
            }
            else
            {
                // When in database mode, we only need database context service
                builder.Services.AddSingleton<IDatabaseContext>(provider => 
                    new DatabaseContextService(provider.GetRequiredService<IDatabaseService>()));
            }

            // Register MCP server
            var mcpServerBuilder = builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new()
                    {
                        Name = "MSSQLClient",
                        Version = GetServerVersion()
                    };
                })
                .WithStdioServerTransport();

            Console.Error.WriteLine("Registering MCP tools...");
            
            // Register common tools that work in both modes
            mcpServerBuilder.WithTools<ServerCapabilitiesTool>();
            Console.Error.WriteLine("Registered ServerCapabilitiesTool");

            if (isServerMode)
            {
                // Server mode tools
                Console.Error.WriteLine("Registering server mode tools...");
                
                // Table-related tools
                mcpServerBuilder.WithTools<ServerListTablesTool>();
                Console.Error.WriteLine("Registered ServerListTablesTool");
                
                mcpServerBuilder.WithTools<ServerListDatabasesTool>();
                Console.Error.WriteLine("Registered ServerListDatabasesTool");
                
                mcpServerBuilder.WithTools<ServerGetTableSchemaTool>();
                Console.Error.WriteLine("Registered ServerGetTableSchemaTool");
                
                // Stored procedure tools
                mcpServerBuilder.WithTools<ServerListStoredProceduresTool>();
                Console.Error.WriteLine("Registered ServerListStoredProceduresTool");
                
                mcpServerBuilder.WithTools<ServerGetStoredProcedureDefinitionTool>();
                Console.Error.WriteLine("Registered ServerGetStoredProcedureDefinitionTool");
                
                mcpServerBuilder.WithTools<ServerGetStoredProcedureParametersTool>();
                Console.Error.WriteLine("Registered ServerGetStoredProcedureParametersTool");
                
                // Only register execute query tool if it's enabled in configuration
                if (dbConfig.EnableExecuteQuery)
                {
                    mcpServerBuilder.WithTools<ServerExecuteQueryTool>();
                    Console.Error.WriteLine("Registered ServerExecuteQueryTool");
                }
                else
                {
                    Console.Error.WriteLine("ServerExecuteQueryTool registration skipped (EnableExecuteQuery is false)");
                }
                
                // Only register execute stored procedure tool if it's enabled in configuration
                if (dbConfig.EnableExecuteStoredProcedure)
                {
                    mcpServerBuilder.WithTools<ServerExecuteStoredProcedureTool>();
                    Console.Error.WriteLine("Registered ServerExecuteStoredProcedureTool");
                }
                else
                {
                    Console.Error.WriteLine("ServerExecuteStoredProcedureTool registration skipped (EnableExecuteStoredProcedure is false)");
                }
            }
            else
            {
                // Database mode tools
                Console.Error.WriteLine("Registering database mode tools...");
                
                // Table-related tools
                mcpServerBuilder.WithTools<ListTablesTool>();
                Console.Error.WriteLine("Registered ListTablesTool");
                
                mcpServerBuilder.WithTools<GetTableSchemaTool>();
                Console.Error.WriteLine("Registered GetTableSchemaTool");
                
                // Stored procedure tools
                mcpServerBuilder.WithTools<ListStoredProceduresTool>();
                Console.Error.WriteLine("Registered ListStoredProceduresTool");
                
                mcpServerBuilder.WithTools<GetStoredProcedureDefinitionTool>();
                Console.Error.WriteLine("Registered GetStoredProcedureDefinitionTool");
                
                mcpServerBuilder.WithTools<GetStoredProcedureParametersTool>();
                Console.Error.WriteLine("Registered GetStoredProcedureParametersTool");
                
                // Only register execute query tool if it's enabled in configuration
                if (dbConfig.EnableExecuteQuery)
                {
                    mcpServerBuilder.WithTools<ExecuteQueryTool>();
                    Console.Error.WriteLine("Registered ExecuteQueryTool");
                }
                else
                {
                    Console.Error.WriteLine("ExecuteQueryTool registration skipped (EnableExecuteQuery is false)");
                }
                
                // Only register execute stored procedure tool if it's enabled in configuration
                if (dbConfig.EnableExecuteStoredProcedure)
                {
                    mcpServerBuilder.WithTools<ExecuteStoredProcedureTool>();
                    Console.Error.WriteLine("Registered ExecuteStoredProcedureTool");
                }
                else
                {
                    Console.Error.WriteLine("ExecuteStoredProcedureTool registration skipped (EnableExecuteStoredProcedure is false)");
                }
            }
            
            Console.Error.WriteLine("All tools registered. Building MCP server..."); 

            await builder.Build().RunAsync();
        }
    }
}