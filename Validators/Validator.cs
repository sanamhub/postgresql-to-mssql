using System.Data;
using Application.Helpers;
using Application.Providers.Interfaces;
using Application.Validators.Interfaces;

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
    sqlServerStart:
        using (var sqlServerConnection = _provider.GetSqlServerConnection())
        {
            if (!IsServerConnected(sqlServerConnection))
            {
                Console.WriteLine("Invalid SQL Server Connection!");

                Console.WriteLine("Provide the valid SQL Server Connection String...");
                var connectionString = Console.ReadLine();
                EnvironmentVariableHelper.Set("SqlServerConnectionString", connectionString);

                Console.WriteLine("SqlServerConnectionString Set Successfully!");

                if (!IsServerConnected(sqlServerConnection)) goto sqlServerStart;
            }
        }

    postgreSqlStart:
        using (var postgreSqlConnection = _provider.GetPostgresqlConnection())
        {
            if (!IsServerConnected(postgreSqlConnection))
            {
                Console.WriteLine("Invalid PostgreSQL Connection!");

                Console.WriteLine("Provide the valid PostgreSQL Connection String...");
                var connectionString = Console.ReadLine();
                EnvironmentVariableHelper.Set("PostgresqlConnectionString", connectionString);

                Console.WriteLine("PostgresqlConnectionString Set Successfully!");

                if (!IsServerConnected(postgreSqlConnection)) goto postgreSqlStart;
            }
        }
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
