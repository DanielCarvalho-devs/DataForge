namespace DataForge.Application.Interfaces;

public interface IQualityScoreService
{
    Task<decimal> RecalcularAsync(int idDataset);

    Task<decimal> CalcularCompletudeAsync(int idDataset);

    Task<decimal> CalcularUnicidadeAsync(int idDataset);
}
