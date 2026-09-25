using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DatasetService : IDatasetService
{
    private readonly DataForgeDbContext _context;

    public DatasetService(DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DatasetDto>> ObterPorProjetoAsync(
        int idProjeto,
        int idUtilizador)
    {
        var projetoExiste = await _context.Projetos
            .AsNoTracking()
            .AnyAsync(p =>
                p.IdProjeto == idProjeto &&
                p.IdUtilizador == idUtilizador &&
                p.Ativo);

        if (!projetoExiste)
        {
            return Enumerable.Empty<DatasetDto>();
        }

        return await _context.Datasets
            .AsNoTracking()
            .Where(d => d.IdProjeto == idProjeto)
            .OrderByDescending(d => d.DataImportacao)
            .Select(d => new DatasetDto
            {
                IdDataset = d.IdDataset,
                IdProjeto = d.IdProjeto,
                Nome = d.Nome,
                NomeOriginal = d.NomeOriginal,
                TipoArquivo = d.TipoArquivo,
                TamanhoBytes = d.TamanhoBytes,
                TotalRegistos = d.TotalRegistos,
                TotalColunas = d.TotalColunas,
                QualityScore = d.QualityScore,
                Estado = d.Estado,
                DataImportacao = d.DataImportacao,
                DataProcessamento = d.DataProcessamento
            })
            .ToListAsync();
    }

    public async Task<DatasetDto?> ObterPorIdAsync(
        int idDataset,
        int idUtilizador)
    {
        return await _context.Datasets
            .AsNoTracking()
            .Where(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                d.IdProjetoNavigation.Ativo)
            .Select(d => new DatasetDto
            {
                IdDataset = d.IdDataset,
                IdProjeto = d.IdProjeto,
                Nome = d.Nome,
                NomeOriginal = d.NomeOriginal,
                TipoArquivo = d.TipoArquivo,
                TamanhoBytes = d.TamanhoBytes,
                TotalRegistos = d.TotalRegistos,
                TotalColunas = d.TotalColunas,
                QualityScore = d.QualityScore,
                Estado = d.Estado,
                DataImportacao = d.DataImportacao,
                DataProcessamento = d.DataProcessamento
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DatasetDto> CriarAsync(
        int idProjeto,
        int idUtilizador,
        string nome,
        string nomeOriginal,
        string tipoArquivo,
        string caminhoArquivo,
        long tamanhoBytes)
    {
        var projetoExiste = await _context.Projetos
            .AnyAsync(p =>
                p.IdProjeto == idProjeto &&
                p.IdUtilizador == idUtilizador &&
                p.Ativo);

        if (!projetoExiste)
        {
            throw new ArgumentException(
                "Projeto não encontrado ou não pertence ao utilizador autenticado."
            );
        }

        var dataset = new Dataset
        {
            IdProjeto = idProjeto,
            Nome = nome,
            NomeOriginal = nomeOriginal,
            TipoArquivo = tipoArquivo,
            CaminhoArquivo = caminhoArquivo,
            TamanhoBytes = tamanhoBytes,

            // Ainda não fizemos o processamento do ficheiro.
            TotalRegistos = 0,
            TotalColunas = 0,
            QualityScore = null,

            Estado = "Pendente",

            DataImportacao = DateTime.Now,
            DataProcessamento = null
        };

        _context.Datasets.Add(dataset);

        await _context.SaveChangesAsync();

        return Mapear(dataset);
    }

    public async Task<bool> EliminarAsync(
        int idDataset,
        int idUtilizador)
    {
        var dataset = await _context.Datasets
            .Include(d => d.IdProjetoNavigation)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador == idUtilizador);

        if (dataset is null)
        {
            return false;
        }

        _context.Datasets.Remove(dataset);

        await _context.SaveChangesAsync();

        return true;
    }


    public async Task<IEnumerable<DatasetColunaDto>> ObterColunasAsync(
        int idDataset,
        int idUtilizador)
    {
        var datasetExiste =
            await _context.Datasets
                .AsNoTracking()
                .AnyAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                    d.IdProjetoNavigation.Ativo);

        if (!datasetExiste)
        {
            return Enumerable.Empty<DatasetColunaDto>();
        }

        return await _context.DatasetColunas
            .AsNoTracking()
            .Where(c =>
                c.IdDataset == idDataset)
            .OrderBy(c =>
                c.Posicao)
            .Select(c =>
                new DatasetColunaDto
                {
                    IdColuna =
                        c.IdColuna,

                    IdDataset =
                        c.IdDataset,

                    Nome =
                        c.Nome,

                    TipoOriginal =
                        c.TipoOriginal,

                    TipoDetectado =
                        c.TipoDetectado,

                    Posicao =
                        c.Posicao,

                    TotalValores =
                        c.TotalValores,

                    ValoresValidos =
                        c.ValoresValidos,

                    ValoresAusentes =
                        c.ValoresAusentes,

                    ValoresUnicos =
                        c.ValoresUnicos,

                    PercentualValido =
                        c.PercentualValido
                })
            .ToListAsync();
    }
    private static DatasetDto Mapear(Dataset dataset)
    {
        return new DatasetDto
        {
            IdDataset = dataset.IdDataset,
            IdProjeto = dataset.IdProjeto,
            Nome = dataset.Nome,
            NomeOriginal = dataset.NomeOriginal,
            TipoArquivo = dataset.TipoArquivo,
            TamanhoBytes = dataset.TamanhoBytes,
            TotalRegistos = dataset.TotalRegistos,
            TotalColunas = dataset.TotalColunas,
            QualityScore = dataset.QualityScore,
            Estado = dataset.Estado,
            DataImportacao = dataset.DataImportacao,
            DataProcessamento = dataset.DataProcessamento
        };
    }
}

