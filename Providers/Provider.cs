using System.Data;
using System.Data.SqlClient;
using Application.Helpers;
using Application.Providers.Interfaces;
using Npgsql;

namespace Application;

internal class Provider : IProvider
{
    private string _sqlServerConnectionString;
    private string _postgresqlConnectionString;

    public Provider()
    {
        _sqlServerConnectionString = EnvironmentVariableHelper.Get("SqlServerConnectionString");
        _postgresqlConnectionString = EnvironmentVariableHelper.Get("PostgresqlConnectionString");
    }

    public SqlConnection GetSqlServerConnection() => new(_sqlServerConnectionString);

    public IDbConnection GetPostgresqlConnection() => new NpgsqlConnection(_postgresqlConnectionString);
}
