using System.Data;

namespace Application;

internal interface IProvider
{
    IDbConnection GetMssqlConnection();
    IDbConnection GetPostgresqlConnection();
}
