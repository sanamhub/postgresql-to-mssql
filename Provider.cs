using System.Data;
using System.Data.SqlClient;
using Npgsql;

namespace Application;

internal class Provider : IProvider
{
    private string _sqlServerConnectionString;
    private string _postgresqlConnectionString;

    public Provider()
    {
        _sqlServerConnectionString = Environment.GetEnvironmentVariable("SqlServerConnectionString");
        _postgresqlConnectionString = Environment.GetEnvironmentVariable("PostgresqlConnectionString");
    }

    public IDbConnection GetSqlServerConnection() => new SqlConnection(_sqlServerConnectionString);

    public IDbConnection GetPostgresqlConnection() => new NpgsqlConnection(_postgresqlConnectionString);
}
