using DataForge.Application.DTOs.Auth;

namespace DataForge.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegistarAsync(RegistarUtilizadorDto dto);

    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}
