using System.Data;
using System.Data.SqlClient;

namespace Application.Providers.Interfaces;

internal interface IProvider {
    SqlConnection GetSqlServerConnection();

    IDbConnection GetPostgresqlConnection();
}
