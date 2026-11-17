using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MinPalavrasPorFase = 1;
    public const int MaxPalavrasPorFase = 10;
    public const int PadraoPalavrasPorFase = 5;

    public const int MinSegundosTimer = 5;
    public const int MaxSegundosTimer = 60;
    public const int PadraoSegundosTimer = 30;

    const string ChaveSave = "progresso";

    public PalavraData[] TodasPalavras { get; private set; }
    public ProgressoSalvo Progresso { get; private set; }

    public int mundoAtual = 1;

    [Header("Configuração do professor (editável em Play)")]
    [SerializeField, Range(MinPalavrasPorFase, MaxPalavrasPorFase)]
    int palavrasPorFase = PadraoPalavrasPorFase;

    // Quantas palavras tem cada fase. Se o save tiver um valor inválido (ou for de uma
    // versão antiga, sem esse campo), volta para o padrão.
    public int PalavrasPorFase
    {
        get
        {
            int valor = Progresso.palavrasPorFase;
            return (valor < MinPalavrasPorFase || valor > MaxPalavrasPorFase)
                ? PadraoPalavrasPorFase
                : valor;
        }
    }

    // Cria o GameManager sozinho ao dar Play, em QUALQUER cena.
    // Assim dá para testar cada minijogo direto, sem passar pelo Menu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CriarAoIniciar()
    {
        if (Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        TodasPalavras = Resources.LoadAll<PalavraData>("Palavras");
        Carregar();

        palavrasPorFase = PalavrasPorFase; // Inspector mostra o valor salvo
    }

    void OnValidate()
    {
        if (!Application.isPlaying || Progresso == null) return;

        if (palavrasPorFase != Progresso.palavrasPorFase)
            DefinirPalavrasPorFase(palavrasPorFase);
    }

    // ---------- Palavras ----------

    public List<PalavraData> PalavrasDoMundo(int mundo)
    {
        return TodasPalavras.Where(p => p.mundo == mundo).ToList();
    }

    // Devolve true se esta palavra completada rendeu uma figurinha nova.
    public bool RegistrarResultado(PalavraData palavra, bool acertou)
    {
        var registro = Progresso.palavras.Find(r => r.palavra == palavra.texto);
        if (registro == null)
        {
            registro = new RegistroPalavra { palavra = palavra.texto };
            Progresso.palavras.Add(registro);
        }

        bool figurinhaNova = false;

        if (acertou)
        {
            registro.acertos++;
            figurinhaNova = DesbloquearFigurinha(palavra.texto);
        }
        else registro.erros++;

        Salvar();
        return figurinhaNova;
    }

    // ---------- Figurinhas ----------
    
    public bool TemFigurinha(string palavra)
    {
        return Progresso.figurinhas.Contains(palavra);
    }

    // Ganha e ainda não vista no álbum (recebe o selo "NOVA!").
    public bool FigurinhaEhNova(string palavra)
    {
        return Progresso.figurinhasNovas.Contains(palavra);
    }

    public void LimparFigurinhasNovas()
    {
        if (Progresso.figurinhasNovas.Count == 0) return;

        Progresso.figurinhasNovas.Clear();
        Salvar();
    }

    bool DesbloquearFigurinha(string palavra)
    {
        if (Progresso.figurinhas.Contains(palavra)) return false;

        Progresso.figurinhas.Add(palavra);
        Progresso.figurinhasNovas.Add(palavra);
        return true;
    }

    // ---------- Fases e estrelas ----------

    // 3 estrelas: até 20% de erros em relação ao número de palavras (arredondado para cima).
    // 2 estrelas: até 1 erro por palavra, em média.
    // 1 estrela: terminou a fase (sempre ganha pelo menos 1).
    public static int CalcularEstrelas(int erros, int totalPalavras)
    {
        if (erros <= Mathf.CeilToInt(totalPalavras * 0.2f)) return 3;
        if (erros <= totalPalavras) return 2;
        return 1;
    }

    public int EstrelasDaFase(string idFase)
    {
        var registro = Progresso.fases.Find(f => f.id == idFase);
        return registro != null ? registro.estrelas : 0;
    }

    // Guarda só o melhor resultado.
    public void RegistrarFase(string idFase, int estrelas)
    {
        var registro = Progresso.fases.Find(f => f.id == idFase);
        if (registro == null)
        {
            registro = new RegistroFase { id = idFase };
            Progresso.fases.Add(registro);
        }

        if (estrelas > registro.estrelas)
            registro.estrelas = estrelas;

        Salvar();
    }

    // ---------- Configurações do professor ----------

    public void DefinirPalavrasPorFase(int valor)
    {
        Progresso.palavrasPorFase = Mathf.Clamp(valor, MinPalavrasPorFase, MaxPalavrasPorFase);
        palavrasPorFase = Progresso.palavrasPorFase; // mantém o Inspector em sincronia
        Salvar();
    }

    // ---------- Timer por palavra (configuração do professor) ----------

    public bool TimerAtivo
    {
        get { return Progresso.timerAtivo; }
    }

    public int SegundosPorPalavra
    {
        get
        {
            int valor = Progresso.segundosPorPalavra;
            return (valor < MinSegundosTimer || valor > MaxSegundosTimer)
                ? PadraoSegundosTimer
                : valor;
        }
    }

    public void DefinirTimerAtivo(bool ativo)
    {
        Progresso.timerAtivo = ativo;
        Salvar();
    }

    public void DefinirSegundosPorPalavra(int segundos)
    {
        Progresso.segundosPorPalavra = Mathf.Clamp(segundos, MinSegundosTimer, MaxSegundosTimer);
        Salvar();
    }

    // ---------- Save ----------

    public void Salvar()
    {
        PlayerPrefs.SetString(ChaveSave, JsonUtility.ToJson(Progresso));
        PlayerPrefs.Save();
    }

    void Carregar()
    {
        string json = PlayerPrefs.GetString(ChaveSave, "");
        Progresso = string.IsNullOrEmpty(json)
            ? new ProgressoSalvo()
            : JsonUtility.FromJson<ProgressoSalvo>(json);

        // Saves antigos podem não ter listas novas.
        if (Progresso.palavras == null) Progresso.palavras = new List<RegistroPalavra>();
        if (Progresso.fases == null) Progresso.fases = new List<RegistroFase>();
        if (Progresso.figurinhas == null) Progresso.figurinhas = new List<string>();
        if (Progresso.figurinhasNovas == null) Progresso.figurinhasNovas = new List<string>();

        // Palavras já acertadas em testes anteriores também valem figurinha.
        foreach (var registro in Progresso.palavras)
        {
            if (registro.acertos > 0 && !Progresso.figurinhas.Contains(registro.palavra))
                Progresso.figurinhas.Add(registro.palavra);
        }
    }

    // Apaga estrelas, figurinhas e desempenho das palavras. Os ajustes
    // (palavras por fase, timer, som, maiúscula/minúscula) são mantidos.
    public void ResetarProgresso()
    {
        Progresso = new ProgressoSalvo
        {
            palavrasPorFase = Progresso.palavrasPorFase,
            somLigado = Progresso.somLigado,
            letraMinuscula = Progresso.letraMinuscula,
            timerAtivo = Progresso.timerAtivo,
            segundosPorPalavra = Progresso.segundosPorPalavra
        };
        Salvar();
    }
}
