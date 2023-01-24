using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Dapper;

namespace Application;

internal class Service : IService
{
    private readonly IProvider _provider;

    public Service(IProvider provider)
    {
        _provider = provider;
    }

    public void Migrate()
    {
        try
        {
            using var postgresConnection = _provider.GetPostgresqlConnection();
            using var sqlServerConnection = _provider.GetSqlServerConnection();

            postgresConnection.Open();
            sqlServerConnection.Open();

            // get list of schemas
            var getSchemasQuery = "SELECT schema_name FROM information_schema.schemata";
            var schemas = postgresConnection.Query<string>(getSchemasQuery).ToList();

            // remove postgres schemas
            schemas.Remove("information_schema");
            schemas.Remove("pg_catalog");
            schemas.Remove("pg_toast");

            foreach (var sourceSchema in schemas)
            {
                // modify unsupported schemas
                string destinationSchema = sourceSchema;
                if (sourceSchema == "public") destinationSchema = "public_new";
                if (sourceSchema == "tran") destinationSchema = "tran_new";

                // create schema
                var createDestinationSchemaQuery = $"CREATE SCHEMA [{destinationSchema}];";
                sqlServerConnection.Execute(createDestinationSchemaQuery);

                // get list of tables
                var getTablesQuery = $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{sourceSchema}'";
                var tables = postgresConnection.Query<string>(getTablesQuery).ToList();

                foreach (var table in tables)
                {
                    // get the table column's definition
                    var getColumnsQuery = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{table}' AND table_schema = '{sourceSchema}'";
                    var columns = postgresConnection.Query(getColumnsQuery);

                    // create the table in sql server
                    var createTableQuery = $"CREATE TABLE {destinationSchema}.{table} (";
                    createTableQuery += string.Join(", ", columns.Select(c => $"{c.column_name} {MapPostgresToSqlServerType(c.data_type)}"));
                    createTableQuery += ")";
                    sqlServerConnection.Execute(createTableQuery);

                    // fetch data from postgres
                    var data = postgresConnection.Query<object>($"SELECT * FROM {sourceSchema}.{table}").ToList();

                    // bulk copy to sql server
                    using var bulkCopy = new SqlBulkCopy(sqlServerConnection);
                    var dataTable = ToDataTable(data);
                    bulkCopy.DestinationTableName = $"[{destinationSchema}].[{table}]";
                    bulkCopy.WriteToServer(dataTable);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void ValidateProviders()
    {
    sqlServerStart:
        using (var sqlServerConnection = _provider.GetSqlServerConnection())
        {
            if (!IsServerConnected(sqlServerConnection))
            {
                Console.WriteLine("Invalid SQL Server Connection!");

                Console.WriteLine("Provide the valid SQL Server Connection String...");
                var connectionString = Console.ReadLine();
                EnvironmentVariable.Set("SqlServerConnectionString", connectionString);

                Console.WriteLine("SqlServerConnectionString Set Successfully!");

                if (!IsServerConnected(sqlServerConnection)) goto sqlServerStart;
            }
        }

    postgreSqlStart:
        using (var postgreSqlConnection = _provider.GetPostgresqlConnection())
        {
            if (!IsServerConnected(postgreSqlConnection))
            {
                Console.WriteLine("Invalid PostgreSQL Connection!");

                Console.WriteLine("Provide the valid PostgreSQL Connection String...");
                var connectionString = Console.ReadLine();
                EnvironmentVariable.Set("PostgresqlConnectionString", connectionString);

                Console.WriteLine("PostgresqlConnectionString Set Successfully!");

                if (!IsServerConnected(postgreSqlConnection)) goto postgreSqlStart;
            }
        }
    }

    #region Private methods

    private static string MapPostgresToSqlServerType(string postgresType)
    {
        var typeMapping = new Dictionary<string, string>
        {
            { "bigint", "bigint" },
            { "boolean", "bit" },
            { "character", "char" },
            { "character varying", "nvarchar(max)" },
            { "date", "date" },
            { "double precision", "float" },
            { "integer", "int" },
            { "interval", "time" },
            { "numeric", "decimal" },
            { "real", "real" },
            { "smallint", "smallint" },
            { "text", "nvarchar(max)" },
            { "time", "time" },
            { "timestamp", "datetime2" },
            { "timestamptz", "datetimeoffset" },
            { "uuid", "uniqueidentifier" },
            { "bytea", "varbinary(max)" },
            { "bit", "bit" },
            { "bit varying", "varbinary(max)" },
            { "money", "money" },
            { "json", "nvarchar(max)" },
            { "jsonb", "nvarchar(max)" },
            { "cidr", "nvarchar(max)" },
            { "inet", "nvarchar(max)" },
            { "macaddr", "nvarchar(max)" },
            { "tsvector", "nvarchar(max)" },
            { "tsquery", "nvarchar(max)" },
            { "array", "nvarchar(max)" },
            { "domain", "nvarchar(max)" },
        };

        return typeMapping.TryGetValue(postgresType.ToLower(), out string value) ? value : "nvarchar(max)";
    }

    private static bool IsServerConnected(IDbConnection connection)
    {
        try
        {
            connection.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static DataTable ToDataTable<T>(List<T> items)
    {
        var dataTable = new DataTable(typeof(T).Name);

        // Get all properties of the object
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            // Adding column names as property names
            dataTable.Columns.Add(prop.Name, prop.PropertyType);
        }

        foreach (T item in items)
        {
            var values = new object[props.Length];
            for (int i = 0; i < props.Length; i++)
            {
                // Inserting property values to datatable rows
                values[i] = props[i].GetValue(item, null);
            }
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    #endregion
}
