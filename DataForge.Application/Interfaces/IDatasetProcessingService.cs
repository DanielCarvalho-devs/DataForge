namespace DataForge.Application.Interfaces;

public interface IDatasetProcessingService
{
    Task ProcessarAsync(
        int idDataset,
        int idUtilizador
    );
}
