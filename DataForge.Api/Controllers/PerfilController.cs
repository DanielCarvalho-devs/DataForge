using System.Security.Claims;
using System.Security.Cryptography;
using DataForge.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/perfil")]
public class PerfilController : ControllerBase
{
    private const long FotoMaxima = 5L * 1024L * 1024L;

    private readonly DataForgeDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PerfilController(
        DataForgeDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var id = ObterIdUtilizador();

        if (id is null)
            return Unauthorized();

        var utilizador = await _context.Utilizadores
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.IdUtilizador == id.Value &&
                u.Ativo);

        if (utilizador is null)
            return NotFound();

        return Ok(Mapear(utilizador));
    }

    [HttpPut]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarPerfilRequest request)
    {
        var id = ObterIdUtilizador();

        if (id is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest(new
            {
                mensagem = "Informe o nome."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensagem = "Informe o email."
            });
        }

        var nome = request.Nome.Trim();
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        if (nome.Length > 150)
        {
            return BadRequest(new
            {
                mensagem =
                    "O nome pode ter no maximo 150 caracteres."
            });
        }

        if (email.Length > 200 ||
            !email.Contains('@'))
        {
            return BadRequest(new
            {
                mensagem = "Informe um email valido."
            });
        }

        var emailExiste = await _context.Utilizadores
            .AnyAsync(u =>
                u.IdUtilizador != id.Value &&
                u.Email.ToLower() == email);

        if (emailExiste)
        {
            return Conflict(new
            {
                mensagem =
                    "Ja existe um utilizador com este email."
            });
        }

        var utilizador = await _context.Utilizadores
            .FirstOrDefaultAsync(u =>
                u.IdUtilizador == id.Value &&
                u.Ativo);

        if (utilizador is null)
            return NotFound();

        utilizador.Nome = nome;
        utilizador.Email = email;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Perfil atualizado com sucesso.",
            utilizador = Mapear(utilizador)
        });
    }

    [HttpPost("foto")]
    [RequestSizeLimit(FotoMaxima)]
    public async Task<IActionResult> AlterarFoto(
        IFormFile foto)
    {
        var id = ObterIdUtilizador();

        if (id is null)
            return Unauthorized();

        if (foto is null || foto.Length == 0)
        {
            return BadRequest(new
            {
                mensagem = "Selecione uma imagem."
            });
        }

        if (foto.Length > FotoMaxima)
        {
            return BadRequest(new
            {
                mensagem =
                    "A fotografia nao pode exceder 5 MB."
            });
        }

        var extensao =
            Path.GetExtension(foto.FileName)
                .ToLowerInvariant();

        var extensoes =
            new HashSet<string>
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        if (!extensoes.Contains(extensao))
        {
            return BadRequest(new
            {
                mensagem =
                    "Utilize JPG, PNG ou WEBP."
            });
        }

        var contentTypes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        if (!contentTypes.Contains(foto.ContentType))
        {
            return BadRequest(new
            {
                mensagem =
                    "O ficheiro enviado nao e uma imagem valida."
            });
        }

        var utilizador = await _context.Utilizadores
            .FirstOrDefaultAsync(u =>
                u.IdUtilizador == id.Value &&
                u.Ativo);

        if (utilizador is null)
            return NotFound();

        var pasta =
            Path.Combine(
                _environment.ContentRootPath,
                "Storage",
                "Profiles");

        Directory.CreateDirectory(pasta);

        var nome =
            $"{id.Value}_{Guid.NewGuid():N}{extensao}";

        var caminho =
            Path.Combine(pasta, nome);

        await using (var stream =
            new FileStream(
                caminho,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
        {
            await foto.CopyToAsync(stream);
        }

        ApagarFotoAnterior(utilizador.FotoPerfil);

        utilizador.FotoPerfil =
            $"/api/perfil/foto/{nome}";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem =
                "Fotografia atualizada com sucesso.",
            utilizador = Mapear(utilizador)
        });
    }

    [AllowAnonymous]
    [HttpGet("foto/{nome}")]
    public IActionResult ObterFoto(string nome)
    {
        nome = Path.GetFileName(nome);

        var caminho =
            Path.Combine(
                _environment.ContentRootPath,
                "Storage",
                "Profiles",
                nome);

        if (!System.IO.File.Exists(caminho))
            return NotFound();

        var extensao =
            Path.GetExtension(nome)
                .ToLowerInvariant();

        var contentType = extensao switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(
            caminho,
            contentType);
    }

    [HttpDelete("foto")]
    public async Task<IActionResult> RemoverFoto()
    {
        var id = ObterIdUtilizador();

        if (id is null)
            return Unauthorized();

        var utilizador = await _context.Utilizadores
            .FirstOrDefaultAsync(u =>
                u.IdUtilizador == id.Value &&
                u.Ativo);

        if (utilizador is null)
            return NotFound();

        ApagarFotoAnterior(utilizador.FotoPerfil);

        utilizador.FotoPerfil = null;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem =
                "Fotografia removida com sucesso.",
            utilizador = Mapear(utilizador)
        });
    }

    [HttpPut("password")]
    public async Task<IActionResult> AlterarPassword(
        [FromBody] AlterarPasswordRequest request)
    {
        var id = ObterIdUtilizador();

        if (id is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(
            request.PasswordAtual))
        {
            return BadRequest(new
            {
                mensagem =
                    "Informe a password atual."
            });
        }

        if (string.IsNullOrWhiteSpace(
            request.NovaPassword))
        {
            return BadRequest(new
            {
                mensagem =
                    "Informe a nova password."
            });
        }

        if (request.NovaPassword.Length < 8)
        {
            return BadRequest(new
            {
                mensagem =
                    "A nova password deve ter pelo menos 8 caracteres."
            });
        }

        if (request.NovaPassword !=
            request.ConfirmarPassword)
        {
            return BadRequest(new
            {
                mensagem =
                    "A confirmacao da password nao coincide."
            });
        }

        if (request.PasswordAtual ==
            request.NovaPassword)
        {
            return BadRequest(new
            {
                mensagem =
                    "A nova password deve ser diferente da atual."
            });
        }

        var utilizador = await _context.Utilizadores
            .FirstOrDefaultAsync(u =>
                u.IdUtilizador == id.Value &&
                u.Ativo);

        if (utilizador is null)
            return NotFound();

        if (!VerificarPassword(
            request.PasswordAtual,
            utilizador.PasswordHash))
        {
            return BadRequest(new
            {
                mensagem =
                    "A password atual esta incorreta."
            });
        }

        utilizador.PasswordHash =
            CriarPasswordHash(
                request.NovaPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem =
                "Password alterada com sucesso."
        });
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

    private object Mapear(
        DataForge.Infrastructure.Models.Utilizadore u)
    {
        return new
        {
            idUtilizador = u.IdUtilizador,
            nome = u.Nome,
            email = u.Email,
            perfil = u.Perfil,
            ativo = u.Ativo,
            dataCriacao = u.DataCriacao,
            ultimoAcesso = u.UltimoAcesso,
            fotoPerfil = u.FotoPerfil
        };
    }

    private void ApagarFotoAnterior(
        string? fotoPerfil)
    {
        if (string.IsNullOrWhiteSpace(fotoPerfil))
            return;

        var nome =
            Path.GetFileName(fotoPerfil);

        var caminho =
            Path.Combine(
                _environment.ContentRootPath,
                "Storage",
                "Profiles",
                nome);

        if (System.IO.File.Exists(caminho))
        {
            try
            {
                System.IO.File.Delete(caminho);
            }
            catch
            {
                // Uma falha ao apagar a foto antiga
                // nao deve impedir a atualizacao.
            }
        }
    }

    private static string CriarPasswordHash(
        string password)
    {
        const int iteracoes = 100000;

        var salt =
            RandomNumberGenerator.GetBytes(16);

        var hash =
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                32);

        return
            $"{iteracoes}." +
            $"{Convert.ToBase64String(salt)}." +
            $"{Convert.ToBase64String(hash)}";
    }

    private static bool VerificarPassword(
        string password,
        string passwordHash)
    {
        try
        {
            var partes =
                passwordHash.Split('.');

            if (partes.Length != 3)
                return false;

            var iteracoes =
                int.Parse(partes[0]);

            var salt =
                Convert.FromBase64String(
                    partes[1]);

            var esperado =
                Convert.FromBase64String(
                    partes[2]);

            var informado =
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iteracoes,
                    HashAlgorithmName.SHA256,
                    esperado.Length);

            return
                CryptographicOperations
                    .FixedTimeEquals(
                        informado,
                        esperado);
        }
        catch
        {
            return false;
        }
    }
}

public class AtualizarPerfilRequest
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
}

public class AlterarPasswordRequest
{
    public string PasswordAtual { get; set; } = "";
    public string NovaPassword { get; set; } = "";
    public string ConfirmarPassword { get; set; } = "";
}
