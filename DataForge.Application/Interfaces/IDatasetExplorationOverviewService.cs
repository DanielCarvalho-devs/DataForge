using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDatasetExplorationOverviewService
{
    Task<ExploracaoDatasetDto> ObterAsync(
        int idDataset,
        int idUtilizador
    );
}
