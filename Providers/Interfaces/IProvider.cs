using Microsoft.Data.SqlClient;
using System.Data;

namespace Application.Providers.Interfaces;

internal interface IProvider
{
    SqlConnection GetSqlServerConnection();

    IDbConnection GetPostgresqlConnection();
}
