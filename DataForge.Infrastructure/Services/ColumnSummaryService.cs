using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class ColumnSummaryService : IColumnSummaryService
{
    private readonly DataForgeDbContext _context;

    public ColumnSummaryService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<ResumoColunaDto> ObterResumoAsync(
        int idDataset,
        int idColuna,
        int idUtilizador)
    {
        var dataset = await _context.Datasets
            .AsNoTracking()
            .Include(d => d.IdProjetoNavigation)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                d.IdProjetoNavigation.Ativo);

        if (dataset is null)
        {
            throw new KeyNotFoundException(
                "Dataset não encontrado."
            );
        }

        if (dataset.Estado != "Processado")
        {
            throw new InvalidOperationException(
                "O dataset precisa estar processado."
            );
        }

        var coluna = await _context.DatasetColunas
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IdColuna == idColuna &&
                c.IdDataset == idDataset);

        if (coluna is null)
        {
            throw new KeyNotFoundException(
                "Coluna não encontrada neste dataset."
            );
        }

        var estatisticas = await _context.EstatisticasColunas
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.IdColuna == idColuna);

        var total = coluna.TotalValores;
        var validos = coluna.ValoresValidos;
        var ausentes = coluna.ValoresAusentes;
        var unicos = coluna.ValoresUnicos;

        var percentualValido = total == 0
            ? 0m
            : Math.Round(
                validos / (decimal)total * 100m,
                2
            );

        var percentualAusente = total == 0
            ? 0m
            : Math.Round(
                ausentes / (decimal)total * 100m,
                2
            );

        var percentualUnico = validos == 0
            ? 0m
            : Math.Round(
                unicos / (decimal)validos * 100m,
                2
            );

        var papelSemantico =
            SemanticRoleClassifier.Detectar(
                coluna.Nome,
                coluna.TipoDetectado,
                validos,
                unicos
            );

        return new ResumoColunaDto
        {
            IdDataset = dataset.IdDataset,

            IdColuna = coluna.IdColuna,

            Nome = coluna.Nome,

            TipoOriginal = coluna.TipoOriginal,

            TipoDetectado = coluna.TipoDetectado,

            PapelSemantico = papelSemantico,

            Posicao = coluna.Posicao,

            TotalValores = total,

            ValoresValidos = validos,

            ValoresAusentes = ausentes,

            ValoresUnicos = unicos,

            PercentualValido = percentualValido,

            PercentualAusente = percentualAusente,

            PercentualUnico = percentualUnico,

            TemEstatisticasNumericas =
                estatisticas is not null,

            Estatisticas =
                estatisticas is null
                    ? null
                    : new EstatisticasResumoColunaDto
                    {
                        Minimo =
                            estatisticas.Minimo,

                        Maximo =
                            estatisticas.Maximo,

                        Media =
                            estatisticas.Media,

                        Mediana =
                            estatisticas.Mediana,

                        DesvioPadrao =
                            estatisticas.DesvioPadrao,

                        P25 =
                            estatisticas.P25,

                        P50 =
                            estatisticas.P50,

                        P75 =
                            estatisticas.P75,

                        P90 =
                            estatisticas.P90,

                        P99 =
                            estatisticas.P99
                    }
        };
    }

}



