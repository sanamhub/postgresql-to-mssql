using Application.Helpers;
using Application.Providers.Interfaces;
using Application.Services.Interfaces;
using Application.Validators.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using System.Data;

namespace Application.Services;

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
        var errors = new List<string>();

        try
        {
            SpectreConsoleHelper.WriteHeader("postgresql to mssql", Color.Blue);

            _validator.ValidateProviders();

            SpectreConsoleHelper.Log("Initializing...");
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Arrow3)
                .SpinnerStyle(Style.Parse("green"))
                .Start("Starting the migration...", ctx =>
                {
                    using var postgresConnection = _provider.GetPostgresqlConnection();
                    using var sqlServerConnection = _provider.GetSqlServerConnection();

                    postgresConnection.Open();
                    sqlServerConnection.Open();

                    ctx.Status("Fetching postgresql schemas");
                    ctx.Spinner(Spinner.Known.BouncingBall);
                    var getSchemasQuery = "SELECT schema_name FROM information_schema.schemata";
                    var schemas = postgresConnection.Query<string>(getSchemasQuery).ToList();
                    SpectreConsoleHelper.Log("Fetched schemas from postgresql...");

                    RemoveUnnecessarySchemas(schemas);

                    ctx.Status("Looping through available schemas...");
                    foreach (var sourceSchema in schemas)
                    {
                        string destinationSchema = $"{sourceSchema}_new";

                        ctx.Status($"Creating {destinationSchema} schema in sql server...");
                        var createDestinationSchemaQuery = $"CREATE SCHEMA [{destinationSchema}];";
                        sqlServerConnection.Execute(createDestinationSchemaQuery);
                        SpectreConsoleHelper.Log($"Created {destinationSchema} schema in sql server...");

                        ctx.Status($"Fetching available tables from {sourceSchema} schema...");
                        var getTablesQuery = $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{sourceSchema}'";
                        var tables = postgresConnection.Query<string>(getTablesQuery).ToList();
                        SpectreConsoleHelper.Log($"Fetched tables of {sourceSchema} schema from postgres");

                        ctx.Status($"Looping through all tables of {sourceSchema} schema...");
                        foreach (var table in tables)
                        {
                            ctx.Status($"Fetching column definition for {table} table...");
                            var getColumnsQuery = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{table}' AND table_schema = '{sourceSchema}'";
                            var columns = postgresConnection.Query(getColumnsQuery);
                            SpectreConsoleHelper.Log($"Fetched column definition for {table} table...");

                            ctx.Status($"Creating table {destinationSchema}.{table} in sql server...");
                            var createTableQuery = $"CREATE TABLE {destinationSchema}.{table} (";
                            createTableQuery += string.Join(", ", columns.Select(column => $"[{column.column_name}] {ConvertPostgreSqlToSqlServerDataType(column.data_type)}"));
                            createTableQuery += ")";
                            sqlServerConnection.Execute(createTableQuery);
                            SpectreConsoleHelper.Log($"Created table {destinationSchema}.{table} in sql server...");

                            IDataReader data;
                            try
                            {
                                ctx.Status($"Fetching data from {sourceSchema}.{table} from postgresql...");
                                data = postgresConnection.ExecuteReader($"SELECT * FROM {sourceSchema}.{table}");
                                SpectreConsoleHelper.Log($"Fetched data from {sourceSchema}.{table} table of postgresql...");

                                ctx.Status("Coverting the data into proper shape before migrating to sql server...");
                                var dataTable = new DataTable();
                                dataTable.Load(data);
                                SpectreConsoleHelper.Log("Converted data into proper shape...");

                                ctx.Status($"Transferring data from [blue]{sourceSchema}.{table}[/] to [green]{destinationSchema}.{table}[/]");
                                using var bulkCopy = new SqlBulkCopy(sqlServerConnection);
                                bulkCopy.DestinationTableName = $"{destinationSchema}.{table}";
                                bulkCopy.BulkCopyTimeout = 300;
                                bulkCopy.WriteToServer(dataTable);
                                SpectreConsoleHelper.Success($"Successfully transferred data from {sourceSchema}.{table} to {destinationSchema}.{table}");
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"{sourceSchema}~{table}");
                                AnsiConsole.WriteException(ex);
                            }
                        }
                    }
                });
            SpectreConsoleHelper.WriteHeader("Success!", Color.Green);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            if (errors.Count != 0)
            {
                var table = new Table();
                table.Title("List of failed migration table/views");

                table.AddColumn("SourceSchema");
                table.AddColumn("SourceTable");

                foreach (var error in errors)
                {
                    var errorDetails = error.Split("~");
                    table.AddRow(errorDetails[0], errorDetails[1]);
                }

                table.Border(TableBorder.Rounded);
                AnsiConsole.Write(table);
            }
        }
    }

    #region Private methods

    private static void RemoveUnnecessarySchemas(List<string> schemas)
    {
        schemas.Remove("information_schema");
        schemas.Remove("pg_catalog");
        schemas.Remove("pg_toast");
        schemas.Remove("pg_temp_1");
        schemas.Remove("pg_toast_temp_1");
    }

    private static string ConvertPostgreSqlToSqlServerDataType(string postgresDataType)
    {
        var map = new Dictionary<string, string>
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
            { "timestamp with time zone", "datetimeoffset" },
        };

        return map.TryGetValue(postgresDataType.ToLower(), out string? value) ? value.ToUpper() : "nvarchar(max)".ToUpper();
    }

    #endregion
}
