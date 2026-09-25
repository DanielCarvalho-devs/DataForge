using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IDatasetPreviewService
{
    Task<DatasetPreviewDto> ObterAsync(
        int idDataset,
        int idUtilizador,
        int pagina = 1,
        int tamanhoPagina = 25
    );
}
