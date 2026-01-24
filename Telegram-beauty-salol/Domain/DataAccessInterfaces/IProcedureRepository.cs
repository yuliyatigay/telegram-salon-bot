using Domain.Models;

namespace Domain.DataAccessInterfaces;

public interface IProcedureRepository
{
    Task<List<Procedure>> GetAllAsync();
    Task<Procedure> GetByIdAsync(Guid id);
}