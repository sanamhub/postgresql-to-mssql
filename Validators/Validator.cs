using System.Data;
using Application.Helpers;
using Application.Providers.Interfaces;
using Application.Validators.Interfaces;
using Spectre.Console;

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
                SpectreConsoleHelper.Warning("[red]WARN:[/] Invalid sql server connection..");
                SpectreConsoleHelper.Information("[blue]INFO:[/] [red]Provide the valid sql server connection string[/]");
                AnsiConsole.MarkupLine("[green]PROMPT:[/] Type valid [red]sql server connection string[/], SAMPLE: [blue]server=serverName; database=databaseName; user=username; password=password;[/]");

                var connectionString = Console.ReadLine();
                EnvironmentVariableHelper.Set("SqlServerConnectionString", connectionString);
                SpectreConsoleHelper.Success("[white]LOG:[/] Sql server connection string set successfully...");

                if (!IsServerConnected(sqlServerConnection)) goto sqlServerStart;
            }
        }
        SpectreConsoleHelper.Success("[white]LOG:[/] Sql server connected...");

    postgreSqlStart:
        using (var postgreSqlConnection = _provider.GetPostgresqlConnection())
        {
            if (!IsServerConnected(postgreSqlConnection))
            {
                SpectreConsoleHelper.Warning("[red]WARN:[/] Invalid postgresql connection..");
                SpectreConsoleHelper.Information("[blue]INFO:[/] Provide the valid [green]postgresql connection string[/]");
                AnsiConsole.MarkupLine("[green]PROMPT:[/] Type valid [red]postgresql connection string[/], SAMPLE: [blue]Server=127.0.0.1;Port=5432;Database=databaseName;User Id=postgres;Password=password;[/]");

                var connectionString = Console.ReadLine();
                EnvironmentVariableHelper.Set("PostgresqlConnectionString", connectionString);
                SpectreConsoleHelper.Success("[white]LOG:[/] Postgresql connection string set successfully...");

                if (!IsServerConnected(postgreSqlConnection)) goto postgreSqlStart;
            }
        }
        SpectreConsoleHelper.Success("[white]LOG:[/] PostgreSQL connected...");
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
