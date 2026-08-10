using ComplejoDeportivo.Application.DTOs;

namespace ComplejoDeportivo.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO loginRequest);
    }
}