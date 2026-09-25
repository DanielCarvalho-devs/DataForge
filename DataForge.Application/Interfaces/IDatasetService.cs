using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDatasetService
{
    Task<IEnumerable<DatasetDto>> ObterPorProjetoAsync(
        int idProjeto,
        int idUtilizador
    );

    Task<DatasetDto?> ObterPorIdAsync(
        int idDataset,
        int idUtilizador
    );

    Task<DatasetDto> CriarAsync(
        int idProjeto,
        int idUtilizador,
        string nome,
        string nomeOriginal,
        string tipoArquivo,
        string caminhoArquivo,
        long tamanhoBytes
    );

    Task<bool> EliminarAsync(
        int idDataset,
        int idUtilizador
    );

    Task<IEnumerable<DatasetColunaDto>> ObterColunasAsync(
        int idDataset,
        int idUtilizador
    );
}

