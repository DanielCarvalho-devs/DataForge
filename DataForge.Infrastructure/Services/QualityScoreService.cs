using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class QualityScoreService : IQualityScoreService
{
    private const string TipoDuplicado = "Linha Duplicada";

    private readonly DataForgeDbContext _context;

    public QualityScoreService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> RecalcularAsync(
        int idDataset)
    {
        var dataset =
            await _context.Datasets
                .FirstOrDefaultAsync(d =>
                    d.IdDataset == idDataset);

        if (dataset is null)
        {
            throw new KeyNotFoundException(
                $"Dataset {idDataset} não encontrado."
            );
        }

        var completude =
            await CalcularCompletudeAsync(
                idDataset
            );

        var unicidade =
            await CalcularUnicidadeAsync(
                idDataset
            );

        var qualityScore =
            Math.Round(
                (completude + unicidade) / 2m,
                2
            );

        dataset.QualityScore =
            qualityScore;

        await _context.SaveChangesAsync();

        return qualityScore;
    }

    public async Task<decimal> CalcularCompletudeAsync(
        int idDataset)
    {
        var totais =
            await _context.DatasetColunas
                .AsNoTracking()
                .Where(c =>
                    c.IdDataset == idDataset)
                .GroupBy(c => c.IdDataset)
                .Select(g => new
                {
                    TotalCelulas =
                        g.Sum(c => c.TotalValores),

                    ValoresValidos =
                        g.Sum(c => c.ValoresValidos)
                })
                .FirstOrDefaultAsync();

        if (totais is null ||
            totais.TotalCelulas == 0)
        {
            return 100m;
        }

        return Math.Round(
            ((decimal)totais.ValoresValidos /
             totais.TotalCelulas) *
            100m,
            2
        );
    }

    public async Task<decimal> CalcularUnicidadeAsync(
        int idDataset)
    {
        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .Where(d =>
                    d.IdDataset == idDataset)
                .Select(d => new
                {
                    d.TotalRegistos
                })
                .FirstOrDefaultAsync();

        if (dataset is null)
        {
            throw new KeyNotFoundException(
                $"Dataset {idDataset} não encontrado."
            );
        }

        if (dataset.TotalRegistos <= 0)
        {
            return 100m;
        }

        var linhasDuplicadas =
            await _context.Anomaliases
                .AsNoTracking()
                .LongCountAsync(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == TipoDuplicado &&
                    !a.Ignorada);

        var linhasUnicas =
            Math.Max(
                0L,
                dataset.TotalRegistos -
                linhasDuplicadas
            );

        return Math.Round(
            ((decimal)linhasUnicas /
             dataset.TotalRegistos) *
            100m,
            2
        );
    }
}
