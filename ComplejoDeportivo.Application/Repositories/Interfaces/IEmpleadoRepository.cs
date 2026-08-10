using ComplejoDeportivo.Domain;

namespace ComplejoDeportivo.Application.Repositories
{
    public interface IEmpleadoRepository
    {
        Task<IEnumerable<Empleado>> GetAllAsync(string? searchTerm = null);
        Task<Empleado?> GetByIdAsync(int id);
        Task<Empleado> CreateAsync(Empleado empleado);
        Task<bool> UpdateAsync(Empleado empleado);
        Task<bool> DeleteAsync(int id);
        Task<Empleado?> GetByEmailAsync(string email);
    }
}