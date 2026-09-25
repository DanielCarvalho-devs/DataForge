using System.Security.Claims;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/datasets")]
public class DatasetsController : ControllerBase
{
    private readonly IDatasetService _datasetService;
    private readonly IWebHostEnvironment _environment;
    private readonly IDatasetProcessingService _processingService;
    private readonly IDataQualityService _dataQualityService;
    private readonly IDatasetStatisticsService _statisticsService;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IOutlierDetectionService _outlierDetectionService;
    private readonly IDatasetPreviewService _previewService;
    private readonly IExplorationService _explorationService;
    private readonly IColumnSummaryService _columnSummaryService;
    private readonly IDatasetExplorationOverviewService _explorationOverviewService;

    private const long TamanhoMaximo = 50L * 1024L * 1024L;

    private static readonly HashSet<string> ExtensoesPermitidas =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".csv",
            ".xlsx"
        };

    public DatasetsController(
        IDatasetService datasetService,
        IWebHostEnvironment environment,
        IDatasetProcessingService processingService,
        IDataQualityService dataQualityService,
        IDatasetStatisticsService statisticsService,
        IDuplicateDetectionService duplicateDetectionService,
        IOutlierDetectionService outlierDetectionService,
        IDatasetPreviewService previewService,
        IExplorationService explorationService,
        IColumnSummaryService columnSummaryService,
        IDatasetExplorationOverviewService explorationOverviewService)
    {
        _datasetService = datasetService;
        _environment = environment;
        _processingService = processingService;
        _dataQualityService = dataQualityService;
        _statisticsService = statisticsService;
        _duplicateDetectionService = duplicateDetectionService;
        _outlierDetectionService = outlierDetectionService;
        _previewService = previewService;
        _explorationService = explorationService;
        _columnSummaryService = columnSummaryService;
        _explorationOverviewService = explorationOverviewService;
    }

    [HttpGet("projeto/{idProjeto:int}")]
    public async Task<ActionResult<IEnumerable<DatasetDto>>>
        ObterPorProjeto(int idProjeto)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var datasets =
            await _datasetService.ObterPorProjetoAsync(
                idProjeto,
                idUtilizador.Value
            );

        return Ok(datasets);
    }

    [HttpGet("{idDataset:int}")]
    public async Task<ActionResult<DatasetDto>>
        ObterPorId(int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var dataset =
            await _datasetService.ObterPorIdAsync(
                idDataset,
                idUtilizador.Value
            );

        if (dataset is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(dataset);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(TamanhoMaximo)]
    public async Task<ActionResult<DatasetDto>> Upload(
        [FromForm] int idProjeto,
        [FromForm] IFormFile arquivo,
        [FromForm] string? nome)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new
            {
                mensagem = "Nenhum ficheiro foi enviado."
            });
        }

        if (arquivo.Length > TamanhoMaximo)
        {
            return BadRequest(new
            {
                mensagem = "O ficheiro excede o limite de 50 MB."
            });
        }

        var nomeOriginal =
            Path.GetFileName(arquivo.FileName);

        var extensao =
            Path.GetExtension(nomeOriginal)
                .ToLowerInvariant();

        if (!ExtensoesPermitidas.Contains(extensao))
        {
            return BadRequest(new
            {
                mensagem =
                    "Formato não suportado. Utilize ficheiros CSV ou XLSX."
            });
        }

        var nomeDataset =
            string.IsNullOrWhiteSpace(nome)
                ? Path.GetFileNameWithoutExtension(nomeOriginal)
                : nome.Trim();

        if (nomeDataset.Length > 200)
        {
            return BadRequest(new
            {
                mensagem =
                    "O nome do dataset pode ter no máximo 200 caracteres."
            });
        }

        var pastaRelativa =
            Path.Combine(
                "Storage",
                "Datasets",
                idProjeto.ToString()
            );

        var pastaAbsoluta =
            Path.Combine(
                _environment.ContentRootPath,
                pastaRelativa
            );

        Directory.CreateDirectory(pastaAbsoluta);

        var nomeInterno =
            $"{Guid.NewGuid():N}{extensao}";

        var caminhoAbsoluto =
            Path.Combine(
                pastaAbsoluta,
                nomeInterno
            );

        var caminhoRelativo =
            Path.Combine(
                pastaRelativa,
                nomeInterno
            );

        try
        {
            await using (
                var stream = new FileStream(
                    caminhoAbsoluto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true))
            {
                await arquivo.CopyToAsync(stream);
            }

            var dataset =
                await _datasetService.CriarAsync(
                    idProjeto,
                    idUtilizador.Value,
                    nomeDataset,
                    nomeOriginal,
                    extensao.TrimStart('.'),
                    caminhoRelativo,
                    arquivo.Length
                );

            return CreatedAtAction(
                nameof(ObterPorId),
                new
                {
                    idDataset = dataset.IdDataset
                },
                dataset
            );
        }
        catch (ArgumentException ex)
        {
            if (System.IO.File.Exists(caminhoAbsoluto))
            {
                System.IO.File.Delete(caminhoAbsoluto);
            }

            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch
        {
            if (System.IO.File.Exists(caminhoAbsoluto))
            {
                System.IO.File.Delete(caminhoAbsoluto);
            }

            throw;
        }
    }

    [HttpDelete("{idDataset:int}")]
    public async Task<IActionResult> Eliminar(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var eliminado =
            await _datasetService.EliminarAsync(
                idDataset,
                idUtilizador.Value
            );

        if (!eliminado)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(new
        {
            mensagem = "Dataset eliminado com sucesso."
        });
    }


    [HttpPost("{idDataset:int}/processar")]
    public async Task<IActionResult> Processar(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            await _processingService.ProcessarAsync(
                idDataset,
                idUtilizador.Value
            );

            var dataset =
                await _datasetService.ObterPorIdAsync(
                    idDataset,
                    idUtilizador.Value
                );

            return Ok(new
            {
                mensagem =
                    "Dataset processado com sucesso.",

                dataset
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("{idDataset:int}/colunas")]
    public async Task<ActionResult<IEnumerable<DatasetColunaDto>>>
        ObterColunas(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var dataset =
            await _datasetService.ObterPorIdAsync(
                idDataset,
                idUtilizador.Value
            );

        if (dataset is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        var colunas =
            await _datasetService.ObterColunasAsync(
                idDataset,
                idUtilizador.Value
            );

        return Ok(colunas);
    }

    [HttpPost("{idDataset:int}/analisar-qualidade")]
    public async Task<ActionResult<QualidadeDatasetDto>>
        AnalisarQualidade(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _dataQualityService.AnalisarAsync(
                    idDataset,
                    idUtilizador.Value
                );

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }


    [HttpGet("{idDataset:int}/qualidade")]
    public async Task<ActionResult<QualidadeDatasetDto>>
        ObterQualidade(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var resultado =
            await _dataQualityService.ObterAsync(
                idDataset,
                idUtilizador.Value
            );

        if (resultado is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(resultado);
    }

    [HttpPost("{idDataset:int}/calcular-estatisticas")]
    public async Task<ActionResult<IEnumerable<EstatisticaColunaDto>>>
        CalcularEstatisticas(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _statisticsService.CalcularAsync(
                    idDataset,
                    idUtilizador.Value
                );

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }


    [HttpGet("{idDataset:int}/estatisticas")]
    public async Task<ActionResult<IEnumerable<EstatisticaColunaDto>>>
        ObterEstatisticas(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var resultado =
            await _statisticsService.ObterAsync(
                idDataset,
                idUtilizador.Value
            );

        if (resultado is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(resultado);
    }

    [HttpPost("{idDataset:int}/analisar-duplicados")]
    public async Task<ActionResult<DuplicadosDatasetDto>>
        AnalisarDuplicados(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _duplicateDetectionService
                    .AnalisarAsync(
                        idDataset,
                        idUtilizador.Value
                    );

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }


    [HttpGet("{idDataset:int}/duplicados")]
    public async Task<ActionResult<DuplicadosDatasetDto>>
        ObterDuplicados(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var resultado =
            await _duplicateDetectionService
                .ObterAsync(
                    idDataset,
                    idUtilizador.Value
                );

        if (resultado is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(resultado);
    }
    private int? ObterIdUtilizador()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (int.TryParse(
            claim,
            out var idUtilizador))
        {
            return idUtilizador;
        }

        return null;
    }

    [HttpPost("{idDataset:int}/analisar-outliers")]
    public async Task<ActionResult<OutliersDatasetDto>>
        AnalisarOutliers(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _outlierDetectionService
                    .AnalisarAsync(
                        idDataset,
                        idUtilizador.Value
                    );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (
            InvalidOperationException ex
        )
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (
            NotSupportedException ex
        )
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (
            FileNotFoundException ex
        )
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("{idDataset:int}/outliers")]
    public async Task<ActionResult<OutliersDatasetDto>>
        ObterOutliers(int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var resultado =
            await _outlierDetectionService
                .ObterAsync(
                    idDataset,
                    idUtilizador.Value
                );

        if (resultado is null)
        {
            return NotFound(new
            {
                mensagem = "Dataset não encontrado."
            });
        }

        return Ok(resultado);
    }

    [HttpGet("{idDataset:int}/preview")]
    public async Task<ActionResult<DatasetPreviewDto>>
        ObterPreview(
            int idDataset,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 25)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _previewService
                    .ObterAsync(
                        idDataset,
                        idUtilizador.Value,
                        pagina,
                        tamanhoPagina
                    );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("{idDataset:int}/exploracao/colunas/{idColuna:int}/distribuicao")]
    public async Task<ActionResult<DistribuicaoColunaDto>>
        ObterDistribuicaoColuna(
            int idDataset,
            int idColuna,
            [FromQuery] int top = 10)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _explorationService
                    .ObterDistribuicaoAsync(
                        idDataset,
                        idColuna,
                        idUtilizador.Value,
                        top
                    );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("{idDataset:int}/exploracao/colunas/{idColuna:int}/resumo")]
    public async Task<ActionResult<ResumoColunaDto>>
        ObterResumoColuna(
            int idDataset,
            int idColuna)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _columnSummaryService
                    .ObterResumoAsync(
                        idDataset,
                        idColuna,
                        idUtilizador.Value
                    );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("{idDataset:int}/exploracao")]
    public async Task<ActionResult<ExploracaoDatasetDto>>
        ObterExploracaoDataset(
            int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var resultado =
                await _explorationOverviewService
                    .ObterAsync(
                        idDataset,
                        idUtilizador.Value
                    );

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }
}










