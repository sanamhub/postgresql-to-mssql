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
        _mssqlConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        _postgresqlConnectionString = "Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;";
    }

    public IDbConnection GetMssqlConnection() => new SqlConnection(_mssqlConnectionString);

    public IDbConnection GetPostgresqlConnection() => new NpgsqlConnection(_postgresqlConnectionString);
}
