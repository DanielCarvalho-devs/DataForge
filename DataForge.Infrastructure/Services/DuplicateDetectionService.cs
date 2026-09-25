using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DuplicateDetectionService
    : IDuplicateDetectionService
{
    private const string TipoAnomalia =
        "Linha Duplicada";

    private const string TipoProblema =
        "Linha Duplicada";

    private readonly DataForgeDbContext _context;
    private readonly IQualityScoreService _qualityScoreService;

    public DuplicateDetectionService(
        DataForgeDbContext context,
        IQualityScoreService qualityScoreService)
    {
        _context = context;
        _qualityScoreService = qualityScoreService;
    }

    public async Task<DuplicadosDatasetDto> AnalisarAsync(
        int idDataset,
        int idUtilizador)
    {
        var dataset =
            await _context.Datasets
                .Include(d => d.IdProjetoNavigation)
                .FirstOrDefaultAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador ==
                        idUtilizador &&
                    d.IdProjetoNavigation.Ativo);

        if (dataset is null)
        {
            throw new ArgumentException(
                "Dataset não encontrado."
            );
        }

        if (!string.Equals(
            dataset.Estado,
            "Processado",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "O dataset precisa estar processado antes da deteção de duplicados."
            );
        }

        if (!string.Equals(
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "O Duplicate Detection Engine V1 suporta apenas ficheiros CSV."
            );
        }

        if (string.IsNullOrWhiteSpace(
            dataset.CaminhoArquivo))
        {
            throw new FileNotFoundException(
                "O caminho físico do dataset não está definido."
            );
        }

        var caminhoArquivo =
            ResolverCaminhoArquivo(
                dataset.CaminhoArquivo
            );

        if (!File.Exists(caminhoArquivo))
        {
            throw new FileNotFoundException(
                "O ficheiro físico do dataset não foi encontrado.",
                caminhoArquivo
            );
        }

        // ----------------------------------------------------
        // REMOVER RESULTADOS ANTERIORES DESTA ANALISE
        // ----------------------------------------------------

        var anomaliasAntigas =
            await _context.Anomaliases
                .Where(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == TipoAnomalia &&
                    !a.Ignorada)
                .ToListAsync();

        if (anomaliasAntigas.Count > 0)
        {
            _context.Anomaliases.RemoveRange(
                anomaliasAntigas
            );
        }

        var problemasAntigos =
            await _context.ProblemasQualidades
                .Where(p =>
                    p.IdDataset == idDataset &&
                    p.Tipo == TipoProblema &&
                    !p.Resolvido)
                .ToListAsync();

        if (problemasAntigos.Count > 0)
        {
            _context.ProblemasQualidades.RemoveRange(
                problemasAntigos
            );
        }

        // ----------------------------------------------------
        // LEITURA CSV
        // ----------------------------------------------------

        var config =
            new CsvConfiguration(
                CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
                DetectDelimiter = true
            };

        using var reader =
            new StreamReader(
                caminhoArquivo
            );

        using var csv =
            new CsvReader(
                reader,
                config
            );

        if (!await csv.ReadAsync())
        {
            throw new InvalidOperationException(
                "O ficheiro CSV está vazio."
            );
        }

        csv.ReadHeader();

        var header =
            csv.HeaderRecord;

        if (header is null ||
            header.Length == 0)
        {
            throw new InvalidOperationException(
                "O ficheiro CSV não possui cabeçalho."
            );
        }

        // A chave é criada a partir dos campos já interpretados
        // pelo CsvHelper e serializada como JSON.
        //
        // Isso evita colisões simples causadas por concatenar
        // valores com "|" ou outro separador arbitrário.

        var primeiraOcorrencia =
            new Dictionary<string, long>(
                StringComparer.Ordinal
            );

        var duplicados =
            new List<DuplicadoEncontrado>();

        long numeroLinhaFisica = 1;
        long totalRegistosLidos = 0;

        while (await csv.ReadAsync())
        {
            numeroLinhaFisica++;
            totalRegistosLidos++;

            var campos =
                new string[header.Length];

            for (
                var i = 0;
                i < header.Length;
                i++)
            {
                string? valor = null;

                try
                {
                    valor =
                        csv.GetField(i);
                }
                catch
                {
                    valor = null;
                }

                campos[i] =
                    valor?.Trim()
                    ?? string.Empty;
            }

            var chave =
                JsonSerializer.Serialize(
                    campos
                );

            if (primeiraOcorrencia.TryGetValue(
                chave,
                out var linhaOriginal))
            {
                duplicados.Add(
                    new DuplicadoEncontrado
                    {
                        NumeroLinha =
                            numeroLinhaFisica,

                        LinhaOriginal =
                            linhaOriginal,

                        Valores =
                            campos
                    }
                );
            }
            else
            {
                primeiraOcorrencia[chave] =
                    numeroLinhaFisica;
            }
        }

        // ----------------------------------------------------
        // GRAVAR ANOMALIAS
        // ----------------------------------------------------

        foreach (var duplicado in duplicados)
        {
            var valorOriginal =
                JsonSerializer.Serialize(
                    duplicado.Valores
                );

            // ValorOriginal não tem HasMaxLength no EF,
            // portanto usamos o conteúdo completo da linha.

            _context.Anomaliases.Add(
                new Anomalias
                {
                    IdDataset =
                        idDataset,

                    IdColuna =
                        null,

                    NumeroLinha =
                        duplicado.NumeroLinha,

                    ValorOriginal =
                        valorOriginal,

                    Tipo =
                        TipoAnomalia,

                    Score =
                        1m,

                    Descricao =
                        $"A linha {duplicado.NumeroLinha} " +
                        $"é duplicada da linha " +
                        $"{duplicado.LinhaOriginal}.",

                    Ignorada =
                        false,

                    DataDeteccao =
                        DateTime.Now
                }
            );
        }

        // ----------------------------------------------------
        // PROBLEMA AGREGADO
        // ----------------------------------------------------

        if (duplicados.Count > 0)
        {
            var percentual =
                totalRegistosLidos == 0
                    ? 0m
                    : Math.Round(
                        ((decimal)duplicados.Count /
                        totalRegistosLidos) *
                        100m,
                        2
                    );

            _context.ProblemasQualidades.Add(
                new ProblemasQualidade
                {
                    IdDataset =
                        idDataset,

                    IdColuna =
                        null,

                    Tipo =
                        TipoProblema,

                    Severidade =
                        ObterSeveridade(
                            percentual
                        ),

                    Descricao =
                        $"Foram encontradas " +
                        $"{duplicados.Count} ocorrência(s) " +
                        $"de linhas duplicadas em " +
                        $"{totalRegistosLidos} registos " +
                        $"({percentual:0.00}%).",

                    Quantidade =
                        duplicados.Count,

                    Resolvido =
                        false,

                    DataDeteccao =
                        DateTime.Now,

                    DataResolucao =
                        null
                }
            );
        }

        await _context.SaveChangesAsync();
await _qualityScoreService.RecalcularAsync(
            idDataset
        );

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    public async Task<DuplicadosDatasetDto?> ObterAsync(
        int idDataset,
        int idUtilizador)
    {
        var autorizado =
            await _context.Datasets
                .AsNoTracking()
                .AnyAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador ==
                        idUtilizador &&
                    d.IdProjetoNavigation.Ativo);

        if (!autorizado)
        {
            return null;
        }

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    private async Task<DuplicadosDatasetDto>
        ConstruirResultadoAsync(
            int idDataset)
    {
        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .FirstAsync(d =>
                    d.IdDataset == idDataset);

        var duplicados =
            await _context.Anomaliases
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == TipoAnomalia &&
                    !a.Ignorada)
                .OrderBy(a =>
                    a.NumeroLinha)
                .Select(a =>
                    new AnomaliaDuplicadoDto
                    {
                        IdAnomalia =
                            a.IdAnomalia,

                        NumeroLinha =
                            a.NumeroLinha,

                        Tipo =
                            a.Tipo,

                        ValorOriginal =
                            a.ValorOriginal,

                        Descricao =
                            a.Descricao,

                        Score =
                            a.Score,

                        Ignorada =
                            a.Ignorada,

                        DataDeteccao =
                            a.DataDeteccao
                    })
                .ToListAsync();

        var percentual =
            dataset.TotalRegistos == 0
                ? 0m
                : Math.Round(
                    ((decimal)duplicados.Count /
                    dataset.TotalRegistos) *
                    100m,
                    2
                );

        return new DuplicadosDatasetDto
        {
            IdDataset =
                idDataset,

            TotalRegistos =
                dataset.TotalRegistos,

            LinhasDuplicadas =
                duplicados.Count,

            PercentualDuplicados =
                percentual,

            Duplicados =
                duplicados
        };
    }

    private static string ObterSeveridade(
        decimal percentualDuplicados)
    {
        if (percentualDuplicados > 50m)
        {
            return "Critico";
        }

        if (percentualDuplicados > 20m)
        {
            return "Erro";
        }

        if (percentualDuplicados > 5m)
        {
            return "Aviso";
        }

        return "Informacao";
    }

    private static string ResolverCaminhoArquivo(
        string caminho)
    {
        if (Path.IsPathRooted(caminho))
        {
            return caminho;
        }

        var caminhoBase =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    caminho
                )
            );

        if (File.Exists(caminhoBase))
        {
            return caminhoBase;
        }

        return Path.GetFullPath(
            Path.Combine(
                Directory.GetCurrentDirectory(),
                caminho
            )
        );
    }

    private sealed class DuplicadoEncontrado
    {
        public long NumeroLinha { get; init; }

        public long LinhaOriginal { get; init; }

        public string[] Valores { get; init; }
            = Array.Empty<string>();
    }
}





