using System.Data;
using System.Data.SqlClient;

namespace Application;

internal interface IProvider
{
    SqlConnection GetSqlServerConnection();

    IDbConnection GetPostgresqlConnection();
}
