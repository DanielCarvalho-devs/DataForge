using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IColumnSummaryService
{
    Task<ResumoColunaDto> ObterResumoAsync(
        int idDataset,
        int idColuna,
        int idUtilizador
    );
}
