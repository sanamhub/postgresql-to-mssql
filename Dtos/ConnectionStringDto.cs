namespace Application.Dtos;

internal class ConnectionStringDto {
    public string SqlServerConnectionString { get; set; } = default!;
    public string PostgreSqlConnectionString { get; set; } = default!;
}
