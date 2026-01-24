using Dapper;
using Domain.DataAccessInterfaces;
using Domain.Models;
using Npgsql;

namespace DataAccess;

public class ProcedureRepository : IProcedureRepository
{
    private readonly string _connectionString;

    public ProcedureRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Procedure>> GetAllAsync()
    {
        const string sql = """
                               SELECT *
                               FROM "Procedures"
                               ORDER BY "Id"
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<Procedure>(sql);
        return result.ToList();
    }

    public async Task<Procedure> GetByIdAsync(Guid id)
    {
        const string sql = """
                               SELECT "Id", "Name"
                               FROM "Procedures"
                               WHERE "Id" = @Id
                           """;
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Procedure>(sql, new { Id = id });
    }
}