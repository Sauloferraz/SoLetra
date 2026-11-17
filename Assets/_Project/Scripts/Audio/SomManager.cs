using System;
using System.Collections.Generic;
using UnityEngine;

// Toca os sons do catálogo. Uso em qualquer lugar do código:
//     SomManager.Tocar(SomId.Acerto);
// Cria a si mesmo ao dar Play, respeita o ajuste de som do jogo e usa um pequeno
// "pool" de AudioSources (vários sons ao mesmo tempo, cada um com seu tom).
public class SomManager : MonoBehaviour
{
    const int TamanhoDoPool = 8;
    const string NomeDoCatalogo = "CatalogoSons";

    public static SomManager Instance { get; private set; }

    [Range(0f, 1f)] public float volumeGeral = 1f;

    readonly Dictionary<SomId, SomEntrada> mapa = new Dictionary<SomId, SomEntrada>();
    readonly Dictionary<SomId, float> ultimaVez = new Dictionary<SomId, float>();
    readonly Dictionary<SomId, AudioSource> sonsTocando = new Dictionary<SomId, AudioSource>();
    AudioSource musica;
    SomId musicaAtual;
    AudioSource[] fontes;
    int proxima;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CriarAoIniciar()
    {
        if (Instance == null)
            new GameObject("SomManager").AddComponent<SomManager>();
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
        
        fontes = new AudioSource[TamanhoDoPool];
        for (int i = 0; i < TamanhoDoPool; i++)
        {
            var fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fontes[i] = fonte;
        }
        
        CarregarCatalogo();
        
        musica = gameObject.AddComponent<AudioSource>();
        musica.playOnAwake = false;
        musica.loop = true;
    }

    void CarregarCatalogo()
    {
        mapa.Clear();

        var catalogo = Resources.Load<SomCatalogo>(NomeDoCatalogo);
        if (catalogo == null)
        {
            Debug.LogError("[Sons] Catálogo não encontrado em Resources/" + NomeDoCatalogo +
                           ". Abra o menu SóLetra > Painel de Sons para criá-lo.");
            return;
        }

        foreach (var entrada in catalogo.sons)
        {
            SomId id;
            if (!Enum.TryParse(entrada.nome, out id) || id == SomId.Nenhum)
            {
                Debug.LogWarning("[Sons] '" + entrada.nome + "' não existe em SomId. Gere o SomId.cs pelo Painel de Sons.");
                continue;
            }
            mapa[id] = entrada;
        }
    }

    public static void Tocar(SomId id)
    {
        if (Instance != null) Instance.TocarInterno(id);
    }

    void TocarInterno(SomId id)
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.Progresso.somLigado) return;

        // Impede reproduzir novamente um som que já está tocando.
        if (sonsTocando.TryGetValue(id, out var fonteAtual))
        {
            if (fonteAtual != null && fonteAtual.isPlaying)
                return;

            sonsTocando.Remove(id);
        }

        SomEntrada entrada;
        if (!mapa.TryGetValue(id, out entrada))
        {
            Debug.LogWarning("[Sons] O som '" + id + "' não está no catálogo.");
            return;
        }

        float agora = Time.unscaledTime;
        float ultima;

        if (entrada.intervaloMinimo > 0f && ultimaVez.TryGetValue(id, out ultima) &&
            agora - ultima < entrada.intervaloMinimo)
            return;

        var clip = SortearClipe(entrada);
        if (clip == null) return;

        ultimaVez[id] = agora;

        var fonte = fontes[proxima];
        proxima = (proxima + 1) % fontes.Length;

        fonte.clip = clip;
        fonte.volume = Mathf.Clamp01(entrada.volume * volumeGeral);
        fonte.pitch = 1f + UnityEngine.Random.Range(-entrada.variacaoTom, entrada.variacaoTom);

        fonte.Play();

        // Registra qual AudioSource está reproduzindo esse SomId.
        sonsTocando[id] = fonte;
    }

    static AudioClip SortearClipe(SomEntrada entrada)
    {
        if (entrada.clipes == null || entrada.clipes.Length == 0) return null;

        for (int i = 0; i < entrada.clipes.Length * 2; i++)
        {
            var clip = entrada.clipes[UnityEngine.Random.Range(0, entrada.clipes.Length)];
            if (clip != null) return clip;
        }
        return null;
    }
    
    public static void TocarMusica(SomId id)
    {
        if (Instance != null)
            Instance.TocarMusicaInterno(id);
    }

    void TocarMusicaInterno(SomId id)
    {
        if (musica.isPlaying && musicaAtual == id)
            return;

        if (!mapa.TryGetValue(id, out SomEntrada entrada))
        {
            Debug.LogWarning("[Sons] Música '" + id + "' não encontrada.");
            return;
        }

        var clip = SortearClipe(entrada);
        if (clip == null) return;

        musica.clip = clip;
        musica.volume = Mathf.Clamp01(entrada.volume * volumeGeral);
        musica.pitch = 1f;
        musica.loop = true;

        musicaAtual = id;

        var gm = GameManager.Instance;
        if (gm != null && !gm.Progresso.somLigado)
            return;

        musica.Play();
    }

    public static void PararMusica()
    {
        if (Instance != null)
            Instance.PararMusicaInterno();
    }

    void PararMusicaInterno()
    {
        musica.Stop();
        musica.clip = null;
    }
}
