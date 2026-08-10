using ComplejoDeportivo.Domain;

namespace ComplejoDeportivo.Application.Repositories
{
    public interface IClienteRepository
    {
        Task<IEnumerable<Cliente>> GetAllAsync();
        Task<Cliente?> GetByIdAsync(int id);
        Task<Cliente> CreateAsync(Cliente cliente);
        Task<bool> UpdateAsync(Cliente cliente);
        Task<bool> DeleteAsync(int id);
        Task<bool> DoesDocumentoExistAsync(string? documento);
        Task<bool> DoesTelefonoExistAsync(string? telefono);
        Task<Cliente?> GetByEmailAsync(string? email);
    }
}