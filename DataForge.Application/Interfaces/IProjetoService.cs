using DataForge.Application.DTOs.Projetos;

namespace DataForge.Application.Interfaces;

public interface IProjetoService
{
    Task<IEnumerable<ProjetoDto>> ObterTodosAsync(int idUtilizador);

    Task<ProjetoDto?> ObterPorIdAsync(
        int id,
        int idUtilizador
    );

    Task<ProjetoDto> CriarAsync(
        int idUtilizador,
        CriarProjetoDto dto
    );

    Task<ProjetoDto?> AtualizarAsync(
        int id,
        int idUtilizador,
        AtualizarProjetoDto dto
    );

    Task<bool> EliminarAsync(
        int id,
        int idUtilizador
    );
}
