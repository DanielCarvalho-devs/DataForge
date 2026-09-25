using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDataQualityService
{
    Task<QualidadeDatasetDto> AnalisarAsync(
        int idDataset,
        int idUtilizador
    );

    Task<QualidadeDatasetDto?> ObterAsync(
        int idDataset,
        int idUtilizador
    );
}
