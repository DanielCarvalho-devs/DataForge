using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class OutlierDetectionService
    : IOutlierDetectionService
{
    private const string TipoAnomalia =
        "Outlier Numérico";

    private const string TipoProblema =
        "Outlier Numérico";

    private readonly DataForgeDbContext _context;

    public OutlierDetectionService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<OutliersDatasetDto>
        AnalisarAsync(
            int idDataset,
            int idUtilizador)
    {
        var dataset =
            await _context.Datasets
                .Include(d =>
                    d.IdProjetoNavigation)
                .Include(d =>
                    d.DatasetColunas)
                .FirstOrDefaultAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador ==
                        idUtilizador &&
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
                "O dataset precisa estar processado antes da análise de outliers."
            );
        }

        if (!string.Equals(
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "O Outlier Engine V1 suporta apenas ficheiros CSV."
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
        // COLUNAS NUMERICAS
        // ----------------------------------------------------

        var colunasNumericas =
            dataset.DatasetColunas
                .Where(c =>
                    (c.TipoDetectado == "Inteiro" ||
                     c.TipoDetectado == "Decimal") &&
                    !PareceIdentificador(c.Nome))
                .OrderBy(c =>
                    c.Posicao)
                .ToList();

        if (colunasNumericas.Count == 0)
        {
            await LimparResultadosAnteriores(
                idDataset
            );

            await _context.SaveChangesAsync();

            return await ConstruirResultadoAsync(
                idDataset
            );
        }

        // ----------------------------------------------------
        // ESTATISTICAS P25 / P75
        // ----------------------------------------------------

        var idsColunas =
            colunasNumericas
                .Select(c => c.IdColuna)
                .ToList();

        var estatisticas =
            await _context.EstatisticasColunas
                .AsNoTracking()
                .Where(e =>
                    idsColunas.Contains(e.IdColuna) &&
                    e.P25 != null &&
                    e.P75 != null)
                .ToListAsync();

        var estatisticasPorColuna =
            estatisticas.ToDictionary(
                e => e.IdColuna
            );

        var semEstatisticas =
            colunasNumericas
                .Where(c =>
                    !estatisticasPorColuna
                        .ContainsKey(c.IdColuna))
                .Select(c => c.Nome)
                .ToList();

        if (semEstatisticas.Count > 0)
        {
            throw new InvalidOperationException(
                "Existem colunas numéricas sem estatísticas. " +
                "Execute primeiro o Statistical Engine. " +
                "Colunas: " +
                string.Join(", ", semEstatisticas)
            );
        }

        // ----------------------------------------------------
        // IDEMPOTENCIA
        // Remove somente resultados deste engine.
        // ----------------------------------------------------

        await LimparResultadosAnteriores(
            idDataset
        );

        // ----------------------------------------------------
        // LIMITES IQR
        // ----------------------------------------------------

        var limites =
            new Dictionary<int, LimitesIqr>();

        foreach (var coluna in colunasNumericas)
        {
            var estatistica =
                estatisticasPorColuna[
                    coluna.IdColuna
                ];

            var p25 =
                estatistica.P25!.Value;

            var p75 =
                estatistica.P75!.Value;

            var iqr =
                p75 - p25;

            // IQR zero significa ausência de dispersão
            // suficiente para aplicar esta regra.
            if (iqr <= 0m)
            {
                continue;
            }

            limites[coluna.IdColuna] =
                new LimitesIqr
                {
                    P25 = p25,
                    P75 = p75,
                    Iqr = iqr,
                    LimiteInferior =
                        p25 - (1.5m * iqr),
                    LimiteSuperior =
                        p75 + (1.5m * iqr)
                };
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

        long numeroLinha = 1;

        var outliersEncontrados =
            new List<OutlierEncontrado>();

        while (await csv.ReadAsync())
        {
            numeroLinha++;

            foreach (var coluna in colunasNumericas)
            {
                if (!limites.TryGetValue(
                    coluna.IdColuna,
                    out var limite))
                {
                    continue;
                }

                string? valorTexto;

                try
                {
                    valorTexto =
                        csv.GetField(
                            coluna.Posicao
                        );
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    valorTexto))
                {
                    continue;
                }

                if (!decimal.TryParse(
                    valorTexto,
                    NumberStyles.Number |
                    NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out var valor))
                {
                    continue;
                }

                if (
                    valor >= limite.LimiteInferior &&
                    valor <= limite.LimiteSuperior
                )
                {
                    continue;
                }

                var distancia =
                    valor < limite.LimiteInferior
                        ? limite.LimiteInferior - valor
                        : valor - limite.LimiteSuperior;

                var score =
                    limite.Iqr == 0m
                        ? 0m
                        : Math.Round(
                            distancia /
                            limite.Iqr,
                            6,
                            MidpointRounding.AwayFromZero
                        );

                outliersEncontrados.Add(
                    new OutlierEncontrado
                    {
                        Coluna = coluna,
                        NumeroLinha = numeroLinha,
                        ValorTexto = valorTexto,
                        Valor = valor,
                        Limites = limite,
                        Score = score
                    }
                );
            }
        }

        // ----------------------------------------------------
        // PERSISTIR ANOMALIAS
        // ----------------------------------------------------

        foreach (
            var item in outliersEncontrados
        )
        {
            _context.Anomaliases.Add(
                new Anomalias
                {
                    IdDataset =
                        idDataset,

                    IdColuna =
                        item.Coluna.IdColuna,

                    NumeroLinha =
                        item.NumeroLinha,

                    ValorOriginal =
                        item.ValorTexto,

                    Tipo =
                        TipoAnomalia,

                    Score =
                        item.Score,

                    Descricao =
                        $"O valor {item.Valor} da coluna " +
                        $"'{item.Coluna.Nome}' está fora dos " +
                        $"limites IQR " +
                        $"[{item.Limites.LimiteInferior}; " +
                        $"{item.Limites.LimiteSuperior}].",

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

        if (outliersEncontrados.Count > 0)
        {
            var percentual =
                dataset.TotalRegistos <= 0 ||
                colunasNumericas.Count == 0
                    ? 0m
                    : Math.Round(
                        ((decimal)outliersEncontrados.Count /
                        (dataset.TotalRegistos *
                         colunasNumericas.Count))
                        * 100m,
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
                        $"Foram encontrados " +
                        $"{outliersEncontrados.Count} " +
                        $"outlier(s) numérico(s) através " +
                        $"do método IQR.",

                    Quantidade =
                        outliersEncontrados.Count,

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

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    public async Task<OutliersDatasetDto?>
        ObterAsync(
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

    private async Task LimparResultadosAnteriores(
        int idDataset)
    {
        var anomalias =
            await _context.Anomaliases
                .Where(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == TipoAnomalia &&
                    !a.Ignorada)
                .ToListAsync();

        if (anomalias.Count > 0)
        {
            _context.Anomaliases.RemoveRange(
                anomalias
            );
        }

        var problemas =
            await _context.ProblemasQualidades
                .Where(p =>
                    p.IdDataset == idDataset &&
                    p.Tipo == TipoProblema &&
                    !p.Resolvido)
                .ToListAsync();

        if (problemas.Count > 0)
        {
            _context.ProblemasQualidades.RemoveRange(
                problemas
            );
        }
    }

    private async Task<OutliersDatasetDto>
        ConstruirResultadoAsync(
            int idDataset)
    {
        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .FirstAsync(d =>
                    d.IdDataset == idDataset);

        var colunasNumericas =
            await _context.DatasetColunas
                .AsNoTracking()
                .Where(c =>
                    c.IdDataset == idDataset &&
                    (c.TipoDetectado == "Inteiro" ||
                     c.TipoDetectado == "Decimal"))
                .ToListAsync();

        var colunasAnalisadas =
            colunasNumericas
                .Where(c =>
                    !PareceIdentificador(c.Nome))
                .ToList();

        var ids =
            colunasAnalisadas
                .Select(c => c.IdColuna)
                .ToList();

        var estatisticas =
            await _context.EstatisticasColunas
                .AsNoTracking()
                .Where(e =>
                    ids.Contains(e.IdColuna) &&
                    e.P25 != null &&
                    e.P75 != null)
                .ToDictionaryAsync(
                    e => e.IdColuna
                );

        var anomalias =
            await _context.Anomaliases
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == TipoAnomalia &&
                    !a.Ignorada)
                .OrderBy(a =>
                    a.NumeroLinha)
                .ThenBy(a =>
                    a.IdColuna)
                .ToListAsync();

        var nomesColunas =
            colunasAnalisadas.ToDictionary(
                c => c.IdColuna,
                c => c.Nome
            );

        var resultado =
            new List<OutlierDto>();

        foreach (var anomalia in anomalias)
        {
            if (
                anomalia.IdColuna is null ||
                !estatisticas.TryGetValue(
                    anomalia.IdColuna.Value,
                    out var estatistica) ||
                estatistica.P25 is null ||
                estatistica.P75 is null
            )
            {
                continue;
            }

            var p25 =
                estatistica.P25.Value;

            var p75 =
                estatistica.P75.Value;

            var iqr =
                p75 - p25;

            var inferior =
                p25 - (1.5m * iqr);

            var superior =
                p75 + (1.5m * iqr);

            decimal? valorNumerico =
                null;

            if (
                !string.IsNullOrWhiteSpace(
                    anomalia.ValorOriginal) &&
                decimal.TryParse(
                    anomalia.ValorOriginal,
                    NumberStyles.Number |
                    NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out var valor)
            )
            {
                valorNumerico =
                    valor;
            }

            resultado.Add(
                new OutlierDto
                {
                    IdAnomalia =
                        anomalia.IdAnomalia,

                    IdColuna =
                        anomalia.IdColuna.Value,

                    NomeColuna =
                        nomesColunas.TryGetValue(
                            anomalia.IdColuna.Value,
                            out var nome)
                                ? nome
                                : string.Empty,

                    NumeroLinha =
                        anomalia.NumeroLinha,

                    ValorOriginal =
                        anomalia.ValorOriginal,

                    ValorNumerico =
                        valorNumerico,

                    P25 =
                        p25,

                    P75 =
                        p75,

                    Iqr =
                        iqr,

                    LimiteInferior =
                        inferior,

                    LimiteSuperior =
                        superior,

                    Score =
                        anomalia.Score,

                    Descricao =
                        anomalia.Descricao,

                    Ignorada =
                        anomalia.Ignorada,

                    DataDeteccao =
                        anomalia.DataDeteccao
                }
            );
        }

        var percentual =
            dataset.TotalRegistos <= 0 ||
            colunasAnalisadas.Count == 0
                ? 0m
                : Math.Round(
                    ((decimal)resultado.Count /
                    (dataset.TotalRegistos *
                     colunasAnalisadas.Count))
                    * 100m,
                    2
                );

        return new OutliersDatasetDto
        {
            IdDataset =
                idDataset,

            TotalRegistos =
                dataset.TotalRegistos,

            TotalColunasAnalisadas =
                colunasAnalisadas.Count,

            TotalOutliers =
                resultado.Count,

            PercentualOutliers =
                percentual,

            Outliers =
                resultado
        };
    }

    private static bool PareceIdentificador(
        string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return false;
        }

        var normalizado =
            nome.Trim()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "")
                .ToLowerInvariant();

        return
            normalizado == "id" ||
            normalizado.StartsWith("id") ||
            normalizado.EndsWith("id") ||
            normalizado.Contains("codigo") ||
            normalizado.Contains("código");
    }

    private static string ObterSeveridade(
        decimal percentual)
    {
        if (percentual > 20m)
        {
            return "Critico";
        }

        if (percentual > 10m)
        {
            return "Erro";
        }

        if (percentual > 2m)
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

        var diretorioAtual =
            Directory.GetCurrentDirectory();

        return Path.GetFullPath(
            Path.Combine(
                diretorioAtual,
                caminho
            )
        );
    }

    private sealed class LimitesIqr
    {
        public decimal P25 { get; init; }

        public decimal P75 { get; init; }

        public decimal Iqr { get; init; }

        public decimal LimiteInferior { get; init; }

        public decimal LimiteSuperior { get; init; }
    }

    private sealed class OutlierEncontrado
    {
        public DatasetColuna Coluna { get; init; }
            = null!;

        public long NumeroLinha { get; init; }

        public string ValorTexto { get; init; }
            = string.Empty;

        public decimal Valor { get; init; }

        public LimitesIqr Limites { get; init; }
            = null!;

        public decimal Score { get; init; }
    }
}
