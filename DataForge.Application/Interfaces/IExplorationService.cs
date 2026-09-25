using DataForge.Application.DTOs.Datasets;

namespace DataForge.Application.Interfaces;

public interface IExplorationService
{
    Task<DistribuicaoColunaDto> ObterDistribuicaoAsync(
        int idDataset,
        int idColuna,
        int idUtilizador,
        int top = 10
    );
}
