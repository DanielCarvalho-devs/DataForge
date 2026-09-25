namespace DataForge.Infrastructure.Services;

public static class SemanticRoleClassifier
{
    public static string Detectar(
        string nome,
        string? tipoDetectado,
        long valoresValidos,
        long valoresUnicos)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Dimensao";
        }

        var nomeNormalizado =
            nome.Trim().ToLowerInvariant();

        var tipo =
            tipoDetectado?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;


        // ====================================================
        // DATA
        // ====================================================

        if (tipo == "data")
        {
            return "Data";
        }


        // ====================================================
        // IDENTIFICADORES EXPLICITOS
        //
        // IdVenda -> Identificador
        // IdCliente -> Identificador
        // Idade -> NAO corresponde ao padrao Id + PascalCase
        //
        // Duplicados nao alteram o papel semantico.
        // ====================================================

        var padraoIdPascalCase =
            nome.Length >= 3 &&
            nome.StartsWith(
                "Id",
                StringComparison.Ordinal
            ) &&
            char.IsUpper(nome[2]);

        var padraoIdSeparado =
            nomeNormalizado == "id" ||
            nomeNormalizado.StartsWith("id_") ||
            nomeNormalizado.StartsWith("id-") ||
            nomeNormalizado.EndsWith("_id") ||
            nomeNormalizado.EndsWith("-id");

        var nomeCodigo =
            nomeNormalizado == "codigo" ||
            nomeNormalizado == "código" ||
            nomeNormalizado.EndsWith("_codigo") ||
            nomeNormalizado.EndsWith("_código") ||
            nomeNormalizado.EndsWith("-codigo") ||
            nomeNormalizado.EndsWith("-código");

        var nomeUuidOuGuid =
            nomeNormalizado == "uuid" ||
            nomeNormalizado == "guid" ||
            nomeNormalizado.EndsWith("_uuid") ||
            nomeNormalizado.EndsWith("_guid") ||
            nomeNormalizado.EndsWith("-uuid") ||
            nomeNormalizado.EndsWith("-guid");

        if (
            padraoIdPascalCase ||
            padraoIdSeparado ||
            nomeCodigo ||
            nomeUuidOuGuid
        )
        {
            return "Identificador";
        }


        // ====================================================
        // IDENTIFICADOR POR NOME + CARDINALIDADE
        // ====================================================

        var todosValidosSaoUnicos =
            valoresValidos > 0 &&
            valoresValidos == valoresUnicos;

        var nomePareceChave =
            nomeNormalizado.Contains("chave") ||
            nomeNormalizado.Contains("key");

        if (
            nomePareceChave &&
            todosValidosSaoUnicos
        )
        {
            return "Identificador";
        }


        // ====================================================
        // MEDIDA
        // ====================================================

        if (
            tipo == "inteiro" ||
            tipo == "decimal"
        )
        {
            return "Medida";
        }


        // ====================================================
        // DIMENSAO
        // ====================================================

        return "Dimensao";
    }
}
