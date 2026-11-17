using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Funções do Editor para o catálogo de sons: cria o catálogo padrão, valida nomes e gera o SomId.cs.
public static class SomCatalogoEditor
{
    public const string CaminhoCatalogo = "Assets/_Project/Resources/CatalogoSons.asset";
    public const string CaminhoSomId = "Assets/_Project/Scripts/Audio/SomId.cs";
    const string PastaSfx = "Assets/_Project/Audio/SFX/";

    // Sons que o jogo já usa. A ordem é a do SomId.cs inicial.
    static readonly (string nome, string arquivo, SomCategoria categoria, float volume, float variacao, float intervalo)[] Padroes =
    {
        ("Acerto",          "acerto.wav",          SomCategoria.Efeito, 1.0f, 0.03f, 0.00f),
        ("Erro",            "erro.wav",            SomCategoria.Efeito, 0.9f, 0.00f, 0.00f),
        ("PalavraCompleta", "palavra_completa.wav", SomCategoria.Efeito, 1.0f, 0.00f, 0.00f),
        ("Estrela",         "estrela.wav",         SomCategoria.Efeito, 1.0f, 0.00f, 0.00f),
        ("FimDeFase",       "fim.wav",             SomCategoria.Efeito, 1.0f, 0.00f, 0.00f),
        ("FigurinhaNova",   "figurinha.wav",       SomCategoria.Efeito, 1.0f, 0.00f, 0.00f),
        ("TimerTique",      "timer_tique.wav",     SomCategoria.Alerta, 0.7f, 0.00f, 0.20f),
        ("TimerAcabou",     "timer_acabou.wav",    SomCategoria.Alerta, 1.0f, 0.00f, 0.00f),
    };

    // Cria o catálogo sozinho na primeira compilação depois de importar o pacote.
    [InitializeOnLoadMethod]
    static void AoCarregar()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                GarantirCatalogo();
        };
    }

    public static SomCatalogo GarantirCatalogo()
    {
        var catalogo = AssetDatabase.LoadAssetAtPath<SomCatalogo>(CaminhoCatalogo);
        if (catalogo != null) return catalogo;

        catalogo = ScriptableObject.CreateInstance<SomCatalogo>();
        AssetDatabase.CreateAsset(catalogo, CaminhoCatalogo);

        PreencherPadroesFaltantes(catalogo);
        GerarSomId(catalogo);

        Debug.Log("[Sons] Catálogo criado em " + CaminhoCatalogo + ". Edite pelo menu SóLetra > Painel de Sons.");
        return catalogo;
    }

    // Adiciona os sons padrão que faltam e liga os clipes padrão aos que estão sem clipe.
    public static void PreencherPadroesFaltantes(SomCatalogo catalogo)
    {
        foreach (var padrao in Padroes)
        {
            var entrada = catalogo.sons.FirstOrDefault(s => s.nome == padrao.nome);
            if (entrada == null)
            {
                entrada = new SomEntrada
                {
                    nome = padrao.nome,
                    categoria = padrao.categoria,
                    volume = padrao.volume,
                    variacaoTom = padrao.variacao,
                    intervaloMinimo = padrao.intervalo
                };
                catalogo.sons.Add(entrada);
            }

            bool semClipe = entrada.clipes == null || entrada.clipes.All(c => c == null);
            if (semClipe)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaSfx + padrao.arquivo);
                if (clip != null) entrada.clipes = new[] { clip };
            }
        }

        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
    }

    // "meu som - legal!.wav" -> "MeuSomLegal"
    public static string NormalizarNome(string bruto)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return "NovoSom";

        string decomposto = bruto.Normalize(NormalizationForm.FormD);
        var semAcento = new StringBuilder();
        foreach (char c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(c);
        }

        var partes = Regex.Split(semAcento.ToString(), "[^A-Za-z0-9]+").Where(p => p.Length > 0);
        string pascal = string.Concat(partes.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));

        if (pascal.Length == 0) return "NovoSom";
        if (!char.IsLetter(pascal[0])) pascal = "Som" + pascal;
        return pascal;
    }

    public static bool NomeValido(string nome)
    {
        return !string.IsNullOrEmpty(nome) && nome != "Nenhum" && Regex.IsMatch(nome, "^[A-Za-z][A-Za-z0-9_]*$");
    }

    // Escreve o SomId.cs com um item para cada som válido do catálogo.
    // Só grava se o conteúdo mudou (evita recompilar à toa).
    public static void GerarSomId(SomCatalogo catalogo)
    {
        var nomes = catalogo.sons.Select(s => s.nome).Where(NomeValido).Distinct().ToList();

        var sb = new StringBuilder();
        sb.Append("// ARQUIVO GERADO PELO PAINEL DE SONS (menu SóLetra > Painel de Sons).\n");
        sb.Append("// Não edite à mão: adicione e remova sons pelo painel.\n");
        sb.Append("public enum SomId\n{\n    Nenhum = 0,\n");
        for (int i = 0; i < nomes.Count; i++)
            sb.Append("    " + nomes[i] + " = " + (i + 1) + ",\n");
        sb.Append("}\n");

        string novo = sb.ToString();
        string atual = File.Exists(CaminhoSomId) ? File.ReadAllText(CaminhoSomId, Encoding.UTF8) : "";
        if (novo == atual) return;

        File.WriteAllText(CaminhoSomId, novo, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(CaminhoSomId);
    }
}
