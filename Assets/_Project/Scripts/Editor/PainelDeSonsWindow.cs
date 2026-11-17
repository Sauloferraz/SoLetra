using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Painel de Sons (SóLetra > Painel de Sons).
// Adicione, remova, ouça e ajuste os sons do jogo sem mexer em código.
// Cada som vira um item de SomId (gerado automaticamente) e é tocado com:
//     SomManager.Tocar(SomId.NomeDoSom);
public class PainelDeSonsWindow : EditorWindow
{
    const string PastaScripts = "Assets/_Project/Scripts";

    SomCatalogo catalogo;
    SerializedObject so;
    Vector2 rolagem;
    string busca = "";
    int filtro; // 0 = todas as categorias
    readonly HashSet<int> abertos = new HashSet<int>();

    // Ações adiadas: mexer na lista no meio do OnGUI quebraria o layout.
    bool pedirNovo;
    bool pedirGerar;
    bool pedirPreencher;
    int pedirRemover = -1;
    readonly List<AudioClip> clipesSoltos = new List<AudioClip>();

    [MenuItem("SóLetra/Painel de Sons", false, 1002)]
    static void Abrir()
    {
        var janela = GetWindow<PainelDeSonsWindow>("Painel de Sons");
        janela.minSize = new Vector2(620f, 420f);
        janela.Show();
    }

    void OnEnable()
    {
        Carregar();
    }

    void Carregar()
    {
        catalogo = SomCatalogoEditor.GarantirCatalogo();
        so = catalogo != null ? new SerializedObject(catalogo) : null;
    }

    void OnGUI()
    {
        if (catalogo == null || so == null)
        {
            EditorGUILayout.HelpBox("Não foi possível abrir o catálogo de sons.", MessageType.Error);
            if (GUILayout.Button("Tentar de novo")) Carregar();
            return;
        }

        so.Update();
        var lista = so.FindProperty("sons");

        DesenharCabecalho(lista);
        DesenharSincronia();
        DesenharBarra();
        DesenharAreaDeArrastar();

        rolagem = EditorGUILayout.BeginScrollView(rolagem);
        for (int i = 0; i < lista.arraySize; i++)
        {
            var item = lista.GetArrayElementAtIndex(i);
            if (!PassaNoFiltro(item)) continue;
            DesenharItem(lista, item, i);
        }
        EditorGUILayout.EndScrollView();

        if (so.ApplyModifiedProperties())
            EditorUtility.SetDirty(catalogo);

        ExecutarAcoes();
    }

    // ---------- Desenho ----------

    void DesenharCabecalho(SerializedProperty lista)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Painel de Sons", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            lista.arraySize + " som(ns) cadastrado(s)   •   no código: SomManager.Tocar(SomId.Nome);",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(2);
    }

    // Avisa quando o SomId.cs não bate com o catálogo (ex.: nome novo ainda não gerado).
    void DesenharSincronia()
    {
        var doCatalogo = catalogo.sons.Select(s => s.nome).Where(SomCatalogoEditor.NomeValido).Distinct().OrderBy(n => n);
        var doEnum = Enum.GetNames(typeof(SomId)).Where(n => n != "Nenhum").OrderBy(n => n);

        if (doCatalogo.SequenceEqual(doEnum)) return;

        EditorGUILayout.HelpBox(
            "O SomId.cs está diferente do catálogo. Nomes novos só podem ser usados no código depois de gerar o arquivo " +
            "(a Unity recompila em seguida).", MessageType.Warning);
        if (GUILayout.Button("Gerar SomId.cs agora")) pedirGerar = true;
    }

    void DesenharBarra()
    {
        EditorGUILayout.BeginHorizontal();

        busca = EditorGUILayout.TextField("Buscar", busca);

        var opcoes = new List<string> { "Todas" };
        opcoes.AddRange(Enum.GetNames(typeof(SomCategoria)));
        filtro = EditorGUILayout.Popup(filtro, opcoes.ToArray(), GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Novo som")) pedirNovo = true;
        if (GUILayout.Button("Preencher sons padrão que faltam")) pedirPreencher = true;
        EditorGUILayout.EndHorizontal();
    }

    void DesenharAreaDeArrastar()
    {
        var area = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
        GUI.Box(area, "Arraste arquivos de áudio aqui para criar sons novos", EditorStyles.helpBox);

        var evento = Event.current;
        if (!area.Contains(evento.mousePosition)) return;

        if (evento.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evento.Use();
        }
        else if (evento.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var objeto in DragAndDrop.objectReferences)
            {
                var clip = objeto as AudioClip;
                if (clip != null) clipesSoltos.Add(clip);
            }
            evento.Use();
        }
    }

    void DesenharItem(SerializedProperty lista, SerializedProperty item, int indice)
    {
        var nome = item.FindPropertyRelative("nome");
        var categoria = item.FindPropertyRelative("categoria");
        var clipes = item.FindPropertyRelative("clipes");
        var volume = item.FindPropertyRelative("volume");
        var variacao = item.FindPropertyRelative("variacaoTom");
        var intervalo = item.FindPropertyRelative("intervaloMinimo");

        bool valido = SomCatalogoEditor.NomeValido(nome.stringValue) && NomeUnico(lista, indice);
        int quantidade = ClipesPreenchidos(clipes);
        string marca = !valido ? "⚠ " : (quantidade == 0 ? "○ " : "● ");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        bool aberto = abertos.Contains(indice);
        string titulo = marca + nome.stringValue + "     [" + (SomCategoria)categoria.enumValueIndex + " · " + quantidade + " clipe(s)]";
        bool novoAberto = EditorGUILayout.Foldout(aberto, titulo, true);
        if (GUILayout.Button("▶ Ouvir", GUILayout.Width(70f))) Ouvir(clipes);
        if (GUILayout.Button("Copiar código", GUILayout.Width(100f)))
            GUIUtility.systemCopyBuffer = "SomManager.Tocar(SomId." + nome.stringValue + ");";
        if (GUILayout.Button("Remover", GUILayout.Width(70f))) pedirRemover = indice;
        EditorGUILayout.EndHorizontal();

        if (novoAberto) abertos.Add(indice); else abertos.Remove(indice);

        if (novoAberto)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(nome, new GUIContent("Nome (vira SomId.Nome)"));
            if (!valido)
                EditorGUILayout.HelpBox("Nome inválido ou repetido. Use letras e números, sem espaços, começando por letra.", MessageType.Error);
            EditorGUILayout.PropertyField(categoria);
            EditorGUILayout.PropertyField(clipes, new GUIContent("Clipes (com vários, um é sorteado)"), true);
            EditorGUILayout.PropertyField(volume);
            EditorGUILayout.PropertyField(variacao, new GUIContent("Variação de tom"));
            EditorGUILayout.PropertyField(intervalo, new GUIContent("Intervalo mínimo (s)"));
            if (quantidade == 0)
                EditorGUILayout.HelpBox("Este som ainda não tem clipe.", MessageType.Info);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // ---------- Ações (depois de desenhar) ----------

    void ExecutarAcoes()
    {
        bool mudou = false;

        if (pedirRemover >= 0 && pedirRemover < catalogo.sons.Count)
        {
            string nome = catalogo.sons[pedirRemover].nome;
            if (ConfirmarRemocao(nome))
            {
                Undo.RecordObject(catalogo, "Remover som");
                catalogo.sons.RemoveAt(pedirRemover);
                abertos.Clear();
                mudou = true;
            }
        }
        pedirRemover = -1;

        if (pedirNovo)
        {
            Undo.RecordObject(catalogo, "Novo som");
            catalogo.sons.Add(new SomEntrada { nome = NomeLivre("NovoSom") });
            abertos.Add(catalogo.sons.Count - 1);
            pedirNovo = false;
            mudou = true;
        }

        if (clipesSoltos.Count > 0)
        {
            Undo.RecordObject(catalogo, "Novos sons");
            foreach (var clip in clipesSoltos)
            {
                catalogo.sons.Add(new SomEntrada
                {
                    nome = NomeLivre(SomCatalogoEditor.NormalizarNome(clip.name)),
                    clipes = new[] { clip }
                });
            }
            clipesSoltos.Clear();
            mudou = true;
        }

        if (pedirPreencher)
        {
            Undo.RecordObject(catalogo, "Sons padrão");
            SomCatalogoEditor.PreencherPadroesFaltantes(catalogo);
            pedirPreencher = false;
            mudou = true;
        }

        if (mudou || pedirGerar)
        {
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            SomCatalogoEditor.GerarSomId(catalogo);
            so.Update();
            Repaint();
        }
        pedirGerar = false;
    }

    // Antes de remover, avisa se algum script ainda usa o som.
    static bool ConfirmarRemocao(string nome)
    {
        var usos = ArquivosQueUsam(nome);
        string mensagem = usos.Count == 0
            ? "Remover o som \"" + nome + "\"?"
            : "O som \"" + nome + "\" é usado em:\n\n• " + string.Join("\n• ", usos) +
              "\n\nSe remover, esses arquivos deixam de compilar até você trocar o uso. Remover mesmo?";

        return EditorUtility.DisplayDialog("Remover som", mensagem, "Remover", "Cancelar");
    }

    static List<string> ArquivosQueUsam(string nome)
    {
        var usos = new List<string>();
        if (!Directory.Exists(PastaScripts)) return usos;

        var regex = new Regex(@"\bSomId\." + Regex.Escape(nome) + @"\b");
        foreach (string arquivo in Directory.GetFiles(PastaScripts, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(arquivo) == "SomId.cs") continue;
            if (regex.IsMatch(File.ReadAllText(arquivo))) usos.Add(Path.GetFileName(arquivo));
        }
        return usos;
    }

    // ---------- Utilitários ----------

    bool PassaNoFiltro(SerializedProperty item)
    {
        if (filtro > 0 && item.FindPropertyRelative("categoria").enumValueIndex != filtro - 1) return false;

        if (string.IsNullOrWhiteSpace(busca)) return true;
        return item.FindPropertyRelative("nome").stringValue.IndexOf(busca, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool NomeUnico(SerializedProperty lista, int indice)
    {
        string nome = lista.GetArrayElementAtIndex(indice).FindPropertyRelative("nome").stringValue;
        for (int i = 0; i < lista.arraySize; i++)
        {
            if (i != indice && lista.GetArrayElementAtIndex(i).FindPropertyRelative("nome").stringValue == nome)
                return false;
        }
        return true;
    }

    string NomeLivre(string basico)
    {
        string nome = basico;
        int n = 2;
        while (catalogo.sons.Any(s => s.nome == nome)) nome = basico + n++;
        return nome;
    }

    static int ClipesPreenchidos(SerializedProperty clipes)
    {
        int total = 0;
        for (int i = 0; i < clipes.arraySize; i++)
            if (clipes.GetArrayElementAtIndex(i).objectReferenceValue != null) total++;
        return total;
    }

    static void Ouvir(SerializedProperty clipes)
    {
        for (int i = 0; i < clipes.arraySize; i++)
        {
            var clip = clipes.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
            if (clip != null)
            {
                TocarPrevia(clip);
                return;
            }
        }
        Debug.LogWarning("[Painel de Sons] Este som ainda não tem clipe.");
    }

    // Toca no Editor usando a ferramenta interna da Unity (por reflexão).
    static void TocarPrevia(AudioClip clip)
    {
        var tipo = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (tipo == null) return;

        var parar = tipo.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        if (parar != null) parar.Invoke(null, null);

        var tocar = tipo.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
                                   new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        if (tocar != null) tocar.Invoke(null, new object[] { clip, 0, false });
        else Debug.LogWarning("[Painel de Sons] Pré-escuta indisponível nesta versão da Unity. Ouça o clipe pelo Project.");
    }
}
