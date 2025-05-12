using Application.Helpers;
using Application.Providers.Interfaces;
using Application.Validators.Interfaces;
using System.Data;

namespace Application.Validators;

internal class Validator : IValidator
{
    private readonly IProvider _provider;

    public Validator(IProvider provider)
    {
        _provider = provider;
    }

    public void ValidateProviders()
    {
        using (var sqlServerConnection = _provider.GetSqlServerConnection())
        {
            if (!IsServerConnected(sqlServerConnection))
            {
                SpectreConsoleHelper.Error("Invalid sql server connection.. ( Configure properly at connectionString.json )");
                Console.ReadLine();
            }
        }
        SpectreConsoleHelper.Success("Sql server connected...");

        using (var postgreSqlConnection = _provider.GetPostgresqlConnection())
        {
            if (!IsServerConnected(postgreSqlConnection))
            {
                SpectreConsoleHelper.Error("Invalid postgresql connection.. ( Configure properly at connectionString.json )");
                Console.ReadLine();
            }
        }
        SpectreConsoleHelper.Success("PostgreSQL connected...");
    }

    #region Private methods

    private static bool IsServerConnected(IDbConnection connection)
    {
        try
        {
            connection.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion
}
