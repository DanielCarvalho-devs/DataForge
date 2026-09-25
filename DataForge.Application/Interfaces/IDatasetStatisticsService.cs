using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDatasetStatisticsService
{
    Task<IEnumerable<EstatisticaColunaDto>> CalcularAsync(
        int idDataset,
        int idUtilizador
    );

    Task<IEnumerable<EstatisticaColunaDto>?> ObterAsync(
        int idDataset,
        int idUtilizador
    );
}
