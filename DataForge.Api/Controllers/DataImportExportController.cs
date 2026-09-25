using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/data-transfer")]
public class DataImportExportController : ControllerBase
{
    private const long TamanhoMaximo = 50L * 1024L * 1024L;
    private const int MaxSqlRows = 500000;

    private readonly DataForgeDbContext _context;
    private readonly IDatasetService _datasetService;
    private readonly IDatasetProcessingService _processingService;
    private readonly IWebHostEnvironment _environment;

    public DataImportExportController(
        DataForgeDbContext context,
        IDatasetService datasetService,
        IDatasetProcessingService processingService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _datasetService = datasetService;
        _processingService = processingService;
        _environment = environment;
    }

    // ========================================================
    // IMPORTACAO DE FICHEIRO
    // CSV permanece CSV.
    // XLSX e convertido para CSV antes de entrar no pipeline.
    // ========================================================

    [HttpPost("arquivo")]
    [RequestSizeLimit(TamanhoMaximo)]
    public async Task<IActionResult> ImportarArquivo(
        [FromForm] int idProjeto,
        [FromForm] IFormFile arquivo,
        [FromForm] string? nome)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
            return Unauthorized(new { mensagem = "Token invalido." });

        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { mensagem = "Selecione um ficheiro." });

        if (arquivo.Length > TamanhoMaximo)
            return BadRequest(new { mensagem = "O ficheiro excede 50 MB." });

        var projetoExiste = await _context.Projetos
            .AsNoTracking()
            .AnyAsync(p =>
                p.IdProjeto == idProjeto &&
                p.IdUtilizador == idUtilizador.Value &&
                p.Ativo);

        if (!projetoExiste)
            return NotFound(new { mensagem = "Projeto nao encontrado." });

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

        if (extensao != ".csv" && extensao != ".xlsx")
        {
            return BadRequest(new
            {
                mensagem = "Formato nao suportado. Utilize CSV ou XLSX."
            });
        }

        var nomeDataset = string.IsNullOrWhiteSpace(nome)
            ? Path.GetFileNameWithoutExtension(arquivo.FileName)
            : nome.Trim();

        if (nomeDataset.Length > 200)
            return BadRequest(new { mensagem = "Nome demasiado longo." });

        var pastaRelativa = Path.Combine(
            "Storage",
            "Datasets",
            idProjeto.ToString());

        var pastaAbsoluta = Path.Combine(
            _environment.ContentRootPath,
            pastaRelativa);

        Directory.CreateDirectory(pastaAbsoluta);

        var nomeCsv = $"{Guid.NewGuid():N}.csv";
        var caminhoCsvAbsoluto = Path.Combine(pastaAbsoluta, nomeCsv);
        var caminhoCsvRelativo = Path.Combine(pastaRelativa, nomeCsv);

        try
        {
            if (extensao == ".csv")
            {
                await using var output = new FileStream(
                    caminhoCsvAbsoluto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    true);

                await arquivo.CopyToAsync(output);
            }
            else
            {
                await ConverterExcelParaCsvAsync(
                    arquivo,
                    caminhoCsvAbsoluto);
            }

            var info = new FileInfo(caminhoCsvAbsoluto);

            var dataset = await _datasetService.CriarAsync(
                idProjeto,
                idUtilizador.Value,
                nomeDataset,
                Path.GetFileName(arquivo.FileName),
                "csv",
                caminhoCsvRelativo,
                info.Length);

            await _processingService.ProcessarAsync(
                dataset.IdDataset,
                idUtilizador.Value);

            var atualizado = await _datasetService.ObterPorIdAsync(
                dataset.IdDataset,
                idUtilizador.Value);

            return Ok(new
            {
                mensagem = extensao == ".xlsx"
                    ? "Excel importado e convertido para dataset com sucesso."
                    : "CSV importado com sucesso.",
                dataset = atualizado
            });
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(caminhoCsvAbsoluto))
                System.IO.File.Delete(caminhoCsvAbsoluto);

            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // ========================================================
    // SQL SERVER - TESTAR CONEXAO
    // ========================================================

    [HttpPost("sql/testar")]
    public async Task<IActionResult> TestarSql(
        [FromBody] SqlConnectionRequest request)
    {
        try
        {
            await using var connection =
                new SqlConnection(CriarConnectionString(request));

            await connection.OpenAsync();

            return Ok(new
            {
                sucesso = true,
                servidor = connection.DataSource,
                baseDados = connection.Database,
                mensagem = "Ligacao SQL Server estabelecida com sucesso."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                sucesso = false,
                mensagem = SanitizarErroSql(ex.Message)
            });
        }
    }

    // ========================================================
    // SQL SERVER - TABELAS E VIEWS
    // ========================================================

    [HttpPost("sql/objetos")]
    public async Task<IActionResult> ObterObjetosSql(
        [FromBody] SqlConnectionRequest request)
    {
        try
        {
            await using var connection =
                new SqlConnection(CriarConnectionString(request));

            await connection.OpenAsync();

            const string sql = """
                SELECT
                    TABLE_SCHEMA AS SchemaName,
                    TABLE_NAME AS ObjectName,
                    TABLE_TYPE AS ObjectType
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
                ORDER BY TABLE_SCHEMA, TABLE_NAME;
                """;

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            var objetos = new List<object>();

            while (await reader.ReadAsync())
            {
                objetos.Add(new
                {
                    schema = reader.GetString(0),
                    nome = reader.GetString(1),
                    tipo = reader.GetString(2)
                });
            }

            return Ok(objetos);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = SanitizarErroSql(ex.Message)
            });
        }
    }

    // ========================================================
    // SQL SERVER - PREVIEW
    // ========================================================

    [HttpPost("sql/preview")]
    public async Task<IActionResult> PreviewSql(
        [FromBody] SqlImportRequest request)
    {
        try
        {
            var query = ConstruirQuery(request);

            await using var connection =
                new SqlConnection(CriarConnectionString(request));

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(query, connection)
                {
                    CommandTimeout = 30
                };

            await using var reader =
                await command.ExecuteReaderAsync(
                    CommandBehavior.SequentialAccess);

            var colunas = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();

            var linhas =
                new List<Dictionary<string, object?>>();

            var contador = 0;

            while (contador < 20 && await reader.ReadAsync())
            {
                var linha =
                    new Dictionary<string, object?>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    linha[colunas[i]] =
                        reader.IsDBNull(i)
                            ? null
                            : reader.GetValue(i);
                }

                linhas.Add(linha);
                contador++;
            }

            return Ok(new
            {
                colunas,
                registos = linhas,
                totalPreview = linhas.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = SanitizarErroSql(ex.Message)
            });
        }
    }

    // ========================================================
    // SQL SERVER - IMPORTAR
    // Resultado e materializado em CSV.
    // ========================================================

    [HttpPost("sql/importar")]
    public async Task<IActionResult> ImportarSql(
        [FromBody] SqlImportRequest request)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
            return Unauthorized(new { mensagem = "Token invalido." });

        if (request.IdProjeto <= 0)
            return BadRequest(new { mensagem = "Projeto invalido." });

        var projetoExiste = await _context.Projetos
            .AsNoTracking()
            .AnyAsync(p =>
                p.IdProjeto == request.IdProjeto &&
                p.IdUtilizador == idUtilizador.Value &&
                p.Ativo);

        if (!projetoExiste)
            return NotFound(new { mensagem = "Projeto nao encontrado." });

        var query = ConstruirQuery(request);

        var nomeDataset = string.IsNullOrWhiteSpace(request.NomeDataset)
            ? (
                !string.IsNullOrWhiteSpace(request.Tabela)
                    ? request.Tabela
                    : "Importacao SQL"
              )
            : request.NomeDataset.Trim();

        if (nomeDataset.Length > 200)
            return BadRequest(new { mensagem = "Nome demasiado longo." });

        var pastaRelativa = Path.Combine(
            "Storage",
            "Datasets",
            request.IdProjeto.ToString());

        var pastaAbsoluta = Path.Combine(
            _environment.ContentRootPath,
            pastaRelativa);

        Directory.CreateDirectory(pastaAbsoluta);

        var nomeArquivo = $"{Guid.NewGuid():N}.csv";
        var caminhoAbsoluto =
            Path.Combine(pastaAbsoluta, nomeArquivo);

        var caminhoRelativo =
            Path.Combine(pastaRelativa, nomeArquivo);

        try
        {
            await using var connection =
                new SqlConnection(CriarConnectionString(request));

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(query, connection)
                {
                    CommandTimeout = 120
                };

            await using var reader =
                await command.ExecuteReaderAsync(
                    CommandBehavior.SequentialAccess);

            await using var stream =
                new FileStream(
                    caminhoAbsoluto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await using var writer =
                new StreamWriter(
                    stream,
                    new UTF8Encoding(true));

            var config =
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };

            await using var csv =
                new CsvWriter(writer, config);

            var nomesColunas = new List<string>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var nome = reader.GetName(i);

                if (string.IsNullOrWhiteSpace(nome))
                    nome = $"Coluna{i + 1}";

                var original = nome;
                var sufixo = 2;

                while (nomesColunas.Contains(
                    nome,
                    StringComparer.OrdinalIgnoreCase))
                {
                    nome = $"{original}_{sufixo++}";
                }

                nomesColunas.Add(nome);
                csv.WriteField(nome);
            }

            await csv.NextRecordAsync();

            var total = 0;

            while (await reader.ReadAsync())
            {
                if (total >= MaxSqlRows)
                {
                    throw new InvalidOperationException(
                        $"A importacao SQL esta limitada a {MaxSqlRows:N0} registos por operacao.");
                }

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i))
                    {
                        csv.WriteField("");
                    }
                    else
                    {
                        var value = reader.GetValue(i);

                        if (value is DateTime dt)
                            csv.WriteField(dt.ToString("O"));
                        else if (value is byte[] bytes)
                            csv.WriteField(Convert.ToBase64String(bytes));
                        else
                            csv.WriteField(
                                Convert.ToString(
                                    value,
                                    CultureInfo.InvariantCulture));
                    }
                }

                await csv.NextRecordAsync();
                total++;
            }

            await writer.FlushAsync();

            var info = new FileInfo(caminhoAbsoluto);

            var dataset = await _datasetService.CriarAsync(
                request.IdProjeto,
                idUtilizador.Value,
                nomeDataset,
                $"{nomeDataset}.sql.csv",
                "csv",
                caminhoRelativo,
                info.Length);

            await _processingService.ProcessarAsync(
                dataset.IdDataset,
                idUtilizador.Value);

            var atualizado =
                await _datasetService.ObterPorIdAsync(
                    dataset.IdDataset,
                    idUtilizador.Value);

            return Ok(new
            {
                mensagem = "Dados SQL importados com sucesso.",
                origem = "SQL Server",
                registosImportados = total,
                dataset = atualizado
            });
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(caminhoAbsoluto))
                System.IO.File.Delete(caminhoAbsoluto);

            return BadRequest(new
            {
                mensagem = SanitizarErroSql(ex.Message)
            });
        }
    }

    // ========================================================
    // EXPORTAR CSV
    // ========================================================

    [HttpGet("datasets/{idDataset:int}/exportar/csv")]
    public async Task<IActionResult> ExportarCsv(int idDataset)
    {
        var dados = await ObterDatasetFisico(idDataset);

        if (dados is null)
            return NotFound(new { mensagem = "Dataset nao encontrado." });

        if (!System.IO.File.Exists(dados.Value.Caminho))
            return NotFound(new { mensagem = "Ficheiro fisico nao encontrado." });

        var bytes =
            await System.IO.File.ReadAllBytesAsync(
                dados.Value.Caminho);

        return File(
            bytes,
            "text/csv; charset=utf-8",
            NomeSeguro(dados.Value.Nome) + ".csv");
    }

    // ========================================================
    // EXPORTAR XLSX
    // ========================================================

    [HttpGet("datasets/{idDataset:int}/exportar/xlsx")]
    public async Task<IActionResult> ExportarExcel(int idDataset)
    {
        var dados = await ObterDatasetFisico(idDataset);

        if (dados is null)
            return NotFound(new { mensagem = "Dataset nao encontrado." });

        if (!System.IO.File.Exists(dados.Value.Caminho))
            return NotFound(new { mensagem = "Ficheiro fisico nao encontrado." });

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Dados");

        using var reader =
            new StreamReader(dados.Value.Caminho);

        var config =
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                DetectDelimiter = true,
                TrimOptions = TrimOptions.Trim
            };

        using var csv =
            new CsvReader(reader, config);

        if (!await csv.ReadAsync())
            return BadRequest(new { mensagem = "Dataset vazio." });

        csv.ReadHeader();

        var headers =
            csv.HeaderRecord ?? Array.Empty<string>();

        for (var c = 0; c < headers.Length; c++)
        {
            worksheet.Cell(1, c + 1).Value =
                headers[c];

            worksheet.Cell(1, c + 1)
                .Style.Font.Bold = true;
        }

        var row = 2;

        while (await csv.ReadAsync())
        {
            for (var c = 0; c < headers.Length; c++)
            {
                worksheet.Cell(row, c + 1).Value =
                    csv.GetField(c) ?? "";
            }

            row++;
        }

        if (headers.Length > 0)
        {
            worksheet.Range(
                1,
                1,
                Math.Max(1, row - 1),
                headers.Length)
                .CreateTable();

            worksheet.Columns().AdjustToContents(
                1,
                Math.Min(row, 200));
        }

        using var output = new MemoryStream();

        workbook.SaveAs(output);

        return File(
            output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            NomeSeguro(dados.Value.Nome) + ".xlsx");
    }

    // ========================================================
    // HELPERS
    // ========================================================

    private async Task ConverterExcelParaCsvAsync(
        IFormFile arquivo,
        string destino)
    {
        using var input = arquivo.OpenReadStream();
        using var workbook = new XLWorkbook(input);

        var worksheet =
            workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "O Excel nao possui folhas.");

        var range = worksheet.RangeUsed();

        if (range is null)
            throw new InvalidOperationException(
                "A primeira folha do Excel esta vazia.");

        await using var stream =
            new FileStream(
                destino,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

        await using var writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(true));

        var config =
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

        await using var csv =
            new CsvWriter(writer, config);

        var primeiraLinha = range.FirstRowUsed().RowNumber();
        var ultimaLinha = range.LastRowUsed().RowNumber();
        var primeiraColuna = range.FirstColumnUsed().ColumnNumber();
        var ultimaColuna = range.LastColumnUsed().ColumnNumber();

        for (var linha = primeiraLinha;
             linha <= ultimaLinha;
             linha++)
        {
            for (var coluna = primeiraColuna;
                 coluna <= ultimaColuna;
                 coluna++)
            {
                var cell = worksheet.Cell(linha, coluna);

                csv.WriteField(
                    cell.GetFormattedString());
            }

            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();
    }

    private async Task<(string Caminho, string Nome)?>
        ObterDatasetFisico(int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
            return null;

        var dataset = await _context.Datasets
            .AsNoTracking()
            .Include(d => d.IdProjetoNavigation)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador ==
                    idUtilizador.Value &&
                d.IdProjetoNavigation.Ativo);

        if (dataset is null ||
            string.IsNullOrWhiteSpace(dataset.CaminhoArquivo))
        {
            return null;
        }

        var caminho = dataset.CaminhoArquivo;

        if (!Path.IsPathRooted(caminho))
        {
            caminho = Path.Combine(
                _environment.ContentRootPath,
                caminho);
        }

        return (
            Path.GetFullPath(caminho),
            dataset.Nome
        );
    }

    private static string ConstruirQuery(
        SqlImportRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            ValidarSelect(request.Query);

            return request.Query.Trim().TrimEnd(';');
        }

        if (string.IsNullOrWhiteSpace(request.Tabela))
        {
            throw new ArgumentException(
                "Selecione uma tabela/view ou informe uma consulta SELECT.");
        }

        var schema =
            string.IsNullOrWhiteSpace(request.Schema)
                ? "dbo"
                : request.Schema.Trim();

        ValidarIdentificadorSql(schema);
        ValidarIdentificadorSql(request.Tabela);

        return $"SELECT * FROM [{schema}].[{request.Tabela.Trim()}]";
    }

    private static void ValidarSelect(string sql)
    {
        var texto = sql.Trim();

        if (!Regex.IsMatch(
            texto,
            @"^(SELECT|WITH)\b",
            RegexOptions.IgnoreCase))
        {
            throw new ArgumentException(
                "Apenas consultas SELECT sao permitidas.");
        }

        var proibido = new Regex(
            @"\b(INSERT|UPDATE|DELETE|DROP|ALTER|TRUNCATE|MERGE|EXEC|EXECUTE|CREATE|GRANT|REVOKE|DENY|BACKUP|RESTORE|DBCC|BULK|OPENROWSET|OPENDATASOURCE|SHUTDOWN|USE)\b",
            RegexOptions.IgnoreCase);

        if (proibido.IsMatch(texto))
        {
            throw new ArgumentException(
                "A consulta contem uma operacao nao permitida.");
        }

        if (texto.Contains("--") ||
            texto.Contains("/*") ||
            texto.Contains("*/") ||
            texto.Contains(";"))
        {
            throw new ArgumentException(
                "Comentarios e multiplas instrucoes SQL nao sao permitidos.");
        }
    }

    private static void ValidarIdentificadorSql(
        string identificador)
    {
        if (!Regex.IsMatch(
            identificador,
            @"^[A-Za-z0-9_]+$"))
        {
            throw new ArgumentException(
                "Nome de schema/tabela invalido.");
        }
    }

    private static string CriarConnectionString(
        SqlConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Server))
            throw new ArgumentException(
                "Informe o servidor SQL Server.");

        if (string.IsNullOrWhiteSpace(request.Database))
            throw new ArgumentException(
                "Informe a base de dados.");

        var builder =
            new SqlConnectionStringBuilder
            {
                DataSource = request.Server.Trim(),
                InitialCatalog = request.Database.Trim(),
                TrustServerCertificate = true,
                Encrypt = true,
                ConnectTimeout = 10,
                ApplicationName = "DataForge"
            };

        if (request.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException(
                    "Informe utilizador e password SQL.");
            }

            builder.UserID = request.Username;
            builder.Password = request.Password;
            builder.PersistSecurityInfo = false;
        }

        return builder.ConnectionString;
    }

    private int? ObterIdUtilizador()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id
            : null;
    }

    private static string NomeSeguro(string nome)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            nome = nome.Replace(c, '_');

        return string.IsNullOrWhiteSpace(nome)
            ? "dataset"
            : nome;
    }

    private static string SanitizarErroSql(string mensagem)
    {
        mensagem = Regex.Replace(
            mensagem,
            @"Password\s*=\s*[^;]+",
            "Password=***",
            RegexOptions.IgnoreCase);

        return mensagem;
    }
}

public class SqlConnectionRequest
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public bool UseWindowsAuth { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class SqlImportRequest : SqlConnectionRequest
{
    public int IdProjeto { get; set; }
    public string? NomeDataset { get; set; }
    public string? Schema { get; set; }
    public string? Tabela { get; set; }
    public string? Query { get; set; }
}
