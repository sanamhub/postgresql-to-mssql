using System.Data;

namespace Application;

internal interface IProvider
{
    IDbConnection GetSqlServerConnection();

    IDbConnection GetPostgresqlConnection();
}
