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
                               SELECT "Name"
                               FROM "Procedures"
                               ORDER BY "Name"
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<Procedure>(sql);
        return result.ToList();
    }
}