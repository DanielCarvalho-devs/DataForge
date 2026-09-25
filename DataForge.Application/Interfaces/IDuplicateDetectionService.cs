using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDuplicateDetectionService
{
    Task<DuplicadosDatasetDto> AnalisarAsync(
        int idDataset,
        int idUtilizador
    );

    Task<DuplicadosDatasetDto?> ObterAsync(
        int idDataset,
        int idUtilizador
    );
}
