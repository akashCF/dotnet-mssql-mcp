# SQL Server MCP Client

A comprehensive Microsoft SQL Server client implementing the Model Context Protocol (MCP). This server provides extensive SQL Server capabilities including query execution, schema discovery, and stored procedure management through a simple MCP interface.

## Overview

The SQL Server MCP client is built with .NET Core using the Model Context Protocol C# SDK ([github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)). It provides tools for executing SQL queries, managing stored procedures, listing tables, and retrieving comprehensive schema information from SQL Server databases. The server is designed to be lightweight yet powerful, demonstrating how to create a robust MCP server with practical database functionality. It can be deployed either directly on a machine or as a Docker container.

The MCP client operates in one of two modes:
- **Database Mode**: When a specific database is specified in the connection string, only operations within that database context are available
- **Server Mode**: When no database is specified in the connection string, server-wide operations across all databases are available

## Features

### Core Database Operations
- Execute SQL queries on connected SQL Server databases
- List all tables with schema and row count information
- Retrieve detailed schema information for specific tables
- Comprehensive stored procedure management and execution

### Advanced Tools
- Export/import tables as CSV
- Paginated and streaming query support
- Index, view, and function metadata
- Script execution and discovery tools
- Cursor guidance for query navigation

### Security & Configuration
- Configurable tool enablement for security
- Environment-based configuration
- Comprehensive error handling and validation

## Getting Started

### Prerequisites

- .NET 8.0 (for local development/deployment)
- Docker (for container deployment)

### Build Instructions (for development)

1. Clone this repository:
   ```bash
   git clone https://github.com/aadversteeg/mssqlclient-mcp-server.git
   ```

2. Navigate to the project root directory:
   ```bash
   cd mssqlclient-mcp-server
   ```

3. Build the project using:
   ```bash
   dotnet build src/mssqlclient.sln
   ```

## Docker Support

### Docker Hub

The SQL Server MCP Client is available on Docker Hub.

```bash
# Pull the latest version
docker pull aadversteeg/mssqlclient-mcp-server:latest
```

### Manual Docker Build

If you need to build the Docker image yourself:

```bash
# Navigate to the repository root
cd mssqlclient-mcp-server

# Build the Docker image
docker build -f src/Core.Infrastructure.McpServer/Dockerfile -t mssqlclient-mcp-server:latest src/

# Run the locally built image
docker run -d --name mssql-mcp -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" mssqlclient-mcp-server:latest
```

## MCP Protocol Usage

### Client Integration

To connect to the SQL Server MCP Client from your applications:

1. Use the Model Context Protocol C# SDK or any MCP-compatible client
2. Configure your client to connect to the server's endpoint
3. Call the available tools described below

## Available Tools

The available tools differ depending on which mode the server is operating in, with some tools available in both modes:

### Common Tools (Both Modes)

#### server_capabilities
Returns detailed information about the capabilities and features of the connected SQL Server instance.

Example request:
```json
{
  "name": "server_capabilities",
  "parameters": {}
}
```

Example response in Server Mode:
```json
{
  "version": "Microsoft SQL Server 2019",
  "majorVersion": 15,
  "minorVersion": 0,
  "buildNumber": 4123,
  "edition": "Enterprise Edition",
  "isAzureSqlDatabase": false,
  "isAzureVmSqlServer": false,
  "isOnPremisesSqlServer": true,
  "toolMode": "server",
  "features": {
    "supportsPartitioning": true,
    "supportsColumnstoreIndex": true,
    "supportsJson": true,
    "supportsInMemoryOLTP": true,
    "supportsRowLevelSecurity": true,
    "supportsDynamicDataMasking": true,
    "supportsDataCompression": true,
    "supportsDatabaseSnapshots": true,
    "supportsQueryStore": true,
    "supportsResumableIndexOperations": true,
    "supportsGraphDatabase": true,
    "supportsAlwaysEncrypted": true,
    "supportsExactRowCount": true,
    "supportsDetailedIndexMetadata": true,
    "supportsTemporalTables": true
  }
}
```

This tool is useful for:
- Determining which features are available in your SQL Server instance
- Debugging compatibility issues
- Understanding which query patterns will be used
- Verifying whether you're in server or database mode

#### cursor_guide
Provides guidance for navigating large result sets using cursors.

Example request:
```json
{
  "name": "cursor_guide",
  "parameters": { "query": "SELECT * FROM Orders" }
}
```

#### paginated_query
Executes a query and returns results in pages.

Example request:
```json
{
  "name": "paginated_query",
  "parameters": { "query": "SELECT * FROM Orders", "page": 1, "pageSize": 100 }
}
```

#### query_streamer
Streams large query results in chunks.

Example request:
```json
{
  "name": "query_streamer",
  "parameters": { "query": "SELECT * FROM Orders" }
}
```

#### export_table_csv
Exports a table to CSV format.

Example request:
```json
{
  "name": "export_table_csv",
  "parameters": { "tableName": "Customers" }
}
```

#### import_table_csv
Imports data from a CSV file into a table.

Example request:
```json
{
  "name": "import_table_csv",
  "parameters": { "tableName": "Customers", "csvData": "...csv content..." }
}
```

#### run_script
Executes a SQL script.

Example request:
```json
{
  "name": "run_script",
  "parameters": { "script": "CREATE TABLE Test (Id INT)" }
}
```

#### discover
Discovers schema, tables, and other metadata.

Example request:
```json
{
  "name": "discover",
  "parameters": {}
}
```

### Server Mode Tools (No Database in Connection String)

#### server_list_tables
Lists all tables in all databases.

Example request:
```json
{
  "name": "server_list_tables",
  "parameters": { "databaseName": "Northwind" }
}
```

#### server_list_databases
Lists all databases on the server.

Example request:
```json
{
  "name": "server_list_databases",
  "parameters": {}
}
```

#### server_get_table_schema
Gets the schema for a table in a specific database.

Example request:
```json
{
  "name": "server_get_table_schema",
  "parameters": { "databaseName": "Northwind", "tableName": "Customers" }
}
```

#### server_list_stored_procedures
Lists all stored procedures in a database.

Example request:
```json
{
  "name": "server_list_stored_procedures",
  "parameters": { "databaseName": "Northwind" }
}
```

#### server_get_stored_procedure_definition
Gets the SQL definition of a stored procedure in a database.

Example request:
```json
{
  "name": "server_get_stored_procedure_definition",
  "parameters": { "databaseName": "Northwind", "procedureName": "GetCustomerOrders" }
}
```

#### server_get_stored_procedure_parameters
Gets parameter info for a stored procedure in a database.

Example request:
```json
{
  "name": "server_get_stored_procedure_parameters",
  "parameters": { "databaseName": "Northwind", "procedureName": "CreateNewCustomer" }
}
```

#### server_execute_query
Executes a query in a specific database (if enabled).

Example request:
```json
{
  "name": "server_execute_query",
  "parameters": { "databaseName": "Northwind", "query": "SELECT * FROM Customers" }
}
```

#### server_execute_stored_procedure
Executes a stored procedure in a specific database (if enabled).

Example request:
```json
{
  "name": "server_execute_stored_procedure",
  "parameters": { "databaseName": "Northwind", "procedureName": "CreateNewCustomer", "parameters": { "CompanyName": "Acme Corp" } }
}
```

#### get_function_details
Returns metadata about functions in a database.

Example request:
```json
{
  "name": "get_function_details",
  "parameters": { "databaseName": "Northwind" }
}
```

#### get_view_details
Returns metadata about views in a database.

Example request:
```json
{
  "name": "get_view_details",
  "parameters": { "databaseName": "Northwind" }
}
```

#### get_index_details
Returns metadata about indexes in a database.

Example request:
```json
{
  "name": "get_index_details",
  "parameters": { "databaseName": "Northwind" }
}
```

### Database Mode Tools (Database in Connection String)

#### list_tables
Lists all tables in the connected database.

Example request:
```json
{
  "name": "list_tables",
  "parameters": {}
}
```

#### get_table_schema
Gets the schema for a table in the connected database.

Example request:
```json
{
  "name": "get_table_schema",
  "parameters": { "tableName": "Customers" }
}
```

#### list_stored_procedures
Lists all stored procedures in the connected database.

Example request:
```json
{
  "name": "list_stored_procedures",
  "parameters": {}
}
```

#### get_stored_procedure_definition
Gets the SQL definition of a stored procedure in the connected database.

Example request:
```json
{
  "name": "get_stored_procedure_definition",
  "parameters": { "procedureName": "GetCustomerOrders" }
}
```

#### get_stored_procedure_parameters
Gets parameter info for a stored procedure in the connected database.

Example request:
```json
{
  "name": "get_stored_procedure_parameters",
  "parameters": { "procedureName": "CreateNewCustomer" }
}
```

#### execute_query
Executes a query in the connected database (if enabled).

Example request:
```json
{
  "name": "execute_query",
  "parameters": { "query": "SELECT * FROM Customers" }
}
```

#### execute_stored_procedure
Executes a stored procedure in the connected database (if enabled).

Example request:
```json
{
  "name": "execute_stored_procedure",
  "parameters": { "procedureName": "CreateNewCustomer", "parameters": { "CompanyName": "Acme Corp" } }
}
```

## Configuration

### Tool Security Configuration

The server provides granular control over which potentially dangerous operations are available:

#### Query Execution Security

By default, SQL query execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteQuery` configuration setting to `true`.

#### Stored Procedure Execution Security

By default, stored procedure execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteStoredProcedure` configuration setting to `true`.

These can be configured in several ways:

1. In the `appsettings.json` file:
```json
{
  "EnableExecuteQuery": true,
  "EnableExecuteStoredProcedure": true
}
```

2. As environment variables when running the container:
```bash
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;..." \
  aadversteeg/mssqlclient-mcp-server:latest
```

3. In the Claude Desktop configuration:
```json
"mssql": {
  "command": "dotnet",
  "args": [
    "YOUR_PATH_TO_DLL\\Core.Infrastructure.McpServer.dll"
  ],
  "env": {
    "MSSQL_CONNECTIONSTRING": "Server=your_server;...",
    "EnableExecuteQuery": "true",
    "EnableExecuteStoredProcedure": "true"
  }
}
```

4. In the VSCode configuration:
```json
"mssql": {
  "type": "stdio",
  "command": "dotnet",
  "args": [
      "exec",
      "YOUR_PATH_TO_DLL/bin/Debug/net8.0/Core.Infrastructure.McpServer.dll"
  ],
  "env":{ 
      "MSSQL_CONNECTIONSTRING": "Data Source=yourserver;Initial Catalog=databasename;uid=user;Password=*****;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
  }
}
```



When these settings are `false` (the default), the respective execution tools will not be registered and will not be available to clients. This provides additional security layers when you only want to allow read-only operations.

### Database Connection String

The SQL Server connection string is required to connect to your database. This connection string should include server information, authentication details, and any required connection options.

You can set the connection string using the `MSSQL_CONNECTIONSTRING` environment variable:

```bash
# Database Mode with both execution types enabled
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  aadversteeg/mssqlclient-mcp-server:latest

# Server Mode with both execution types enabled
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  aadversteeg/mssqlclient-mcp-server:latest
```

#### Server Mode vs Database Mode

The MCP server automatically detects the mode based on the connection string:

- **Server Mode**: When no database is specified in the connection string (no `Database=` or `Initial Catalog=` parameter)
- **Database Mode**: When a specific database is specified in the connection string

Example connection strings:

```
# Database Mode - Connects to specific database
Server=database.example.com;Database=Northwind;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

# Server Mode - No specific database
Server=database.example.com;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

# Database Mode with Windows Authentication
Server=database.example.com;Database=Northwind;Integrated Security=SSPI;TrustServerCertificate=True;

# Server Mode with specific port
Server=database.example.com,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

If no connection string is provided, the server will return an error message when attempting to use the tools.

> Integrated security will not work from a docker container!

## Configuring Claude Desktop

### Using Local Installation

To configure Claude Desktop to use a locally installed SQL Server MCP client:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "dotnet",
  "args": [
    "YOUR_PATH_TO_DLL\\Core.Infrastructure.McpServer.dll"
  ],
  "env": {
    "MSSQL_CONNECTIONSTRING": "Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;",
    "EnableExecuteQuery": "true",
    "EnableExecuteStoredProcedure": "true"
  }
}
```

2. Save the file and restart Claude Desktop

### Using Docker Container

To use the SQL Server MCP client from a Docker container with Claude Desktop:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "docker",
  "args": [
    "run",
    "--rm",
    "-i",
    "-e", "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;",
    "-e", "EnableExecuteQuery=true",
    "-e", "EnableExecuteStoredProcedure=true",
    "aadversteeg/mssqlclient-mcp-server:latest"
  ]
}
```

2. Save the file and restart Claude Desktop

## Architecture

### Type System

The server includes a sophisticated type mapping system that converts JSON values to appropriate SQL Server types based on stored procedure parameter metadata:

- **Automatic Type Detection**: Uses SQL Server's `sys.parameters` metadata as the authoritative source
- **Rich Type Support**: Handles all major SQL Server data types including varchar, nvarchar, int, decimal, datetime, uniqueidentifier, etc.
- **Validation**: Provides detailed error messages for type mismatches and constraint violations
- **Default Values**: Supports parameters with default values and optional parameters

### Parameter Handling

- **Case-Insensitive**: Parameter names are matched case-insensitively
- **Flexible Naming**: Supports both `@ParameterName` and `ParameterName` formats
- **Normalization**: Automatic parameter name normalization and validation
- **JSON Schema**: Generates JSON Schema compatible output for parameter validation

### Security Model

The server implements a multi-layered security approach:

1. **Tool-Level Security**: Individual tools can be enabled/disabled
2. **Parameter Validation**: All inputs are validated against SQL Server metadata
3. **SQL Injection Protection**: Uses parameterized queries throughout
4. **Connection Security**: Supports all SQL Server authentication methods

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.