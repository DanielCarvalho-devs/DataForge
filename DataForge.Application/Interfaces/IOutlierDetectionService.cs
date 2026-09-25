using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IOutlierDetectionService
{
    Task<OutliersDatasetDto> AnalisarAsync(
        int idDataset,
        int idUtilizador
    );

    Task<OutliersDatasetDto?> ObterAsync(
        int idDataset,
        int idUtilizador
    );
}
