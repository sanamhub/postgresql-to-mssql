﻿using Application.Dtos;
using Application.Providers.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace Application.Providers;

internal class Provider : IProvider
{
    public SqlConnection GetSqlServerConnection() => new(ConnectionString?.SqlServerConnectionString);

    public IDbConnection GetPostgresqlConnection() => new NpgsqlConnection(ConnectionString?.PostgreSqlConnectionString);

    #region Private methods

    private static ConnectionStringDto? ConnectionString
    {
        get
        {
            var text = File.ReadAllText("./connectionString.json");
            return JsonSerializer.Deserialize<ConnectionStringDto>(text);
        }
    }

    #endregion
}
