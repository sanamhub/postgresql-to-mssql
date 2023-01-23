using System.Data;

namespace Application;

internal class Service : IService
{
    private readonly IProvider _provider;
    private IDbConnection _mssqlConnection;
    private IDbConnection _postgresqlConnection;

    public Service(IProvider provider)
    {
        _provider = provider;
        _mssqlConnection = _provider.GetMssqlConnection();
        _postgresqlConnection = _provider.GetPostgresqlConnection();
    }

    public void Migrate()
    {
        Console.WriteLine("Migration initiated!");
        Console.ReadLine();
    }
}
