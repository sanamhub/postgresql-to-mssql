using System.Data;
using System.Data.SqlClient;
using Npgsql;

namespace Application;

internal class Provider : IProvider
{
    private string _mssqlConnectionString;
    private string _postgresqlConnectionString;

    public Provider()
    {
        // todo: get from config
        _mssqlConnectionString = "";
        _postgresqlConnectionString = "";
    }

    public IDbConnection GetMssqlConnection() => new SqlConnection(_mssqlConnectionString);

    public IDbConnection GetPostgresqlConnection() => new NpgsqlConnection(_postgresqlConnectionString);
}
