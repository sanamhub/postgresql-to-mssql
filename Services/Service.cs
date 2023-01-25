using System.Data;
using System.Data.SqlClient;
using Application.Providers.Interfaces;
using Application.Services.Interfaces;
using Application.Validators.Interfaces;
using Dapper;

namespace Application;

internal class Service : IService
{
    private readonly IProvider _provider;
    private readonly IValidator _validator;

    public Service(
        IProvider provider,
        IValidator validator
        )
    {
        _provider = provider;
        _validator = validator;
    }

    public void Migrate()
    {
        try
        {
            _validator.ValidateProviders();

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
                string destinationSchema = $"{sourceSchema}_new";

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
                    var data = postgresConnection.Query<dynamic>($"SELECT * FROM {sourceSchema}.{table}").ToList();

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

    private static DataTable ToDataTable(List<dynamic> items)
    {
        var dataTable = new DataTable("DynamicObject");

        foreach (dynamic item in items)
        {
            if (dataTable.Columns.Count == 0)
            {
                foreach (var property in item)
                {
                    dataTable.Columns.Add(property.Key);
                }
            }
            var values = new object[dataTable.Columns.Count];
            var i = 0;
            foreach (var property in item)
            {
                values[i++] = property.Value;
            }
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    #endregion
}
