using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Minijogo 1: a criança vê a imagem, ouve a palavra e arrasta as letras para os espaços.
public class MonteAPalavraController : MonoBehaviour
{
    // Letras usadas como "distratoras" na bandeja (sem K, W e Y, menos comuns no início).
    const string Alfabeto = "ABCDEFGHIJLMNOPQRSTUVXZ";

    [Header("Referências de cena")]
    [SerializeField] Image imagemPalavra;
    [SerializeField] RectTransform containerSlots;
    [SerializeField] RectTransform containerBandeja;
    [SerializeField] TMP_Text textoProgresso;
    [SerializeField] TelaResultado telaResultado;
    [SerializeField] AudioSource audioSource;
    [SerializeField] TimerPalavra timer;
    [SerializeField] AvisoFigurinha avisoFigurinha;

    [Header("Prefabs")]
    [SerializeField] SlotLetra prefabSlot;
    [SerializeField] LetraArrastavel prefabLetra;

    [Header("Configuração")]
    [SerializeField] int letrasExtras = 2;
    [SerializeField] int errosParaDica = 2;

    readonly List<SlotLetra> slots = new List<SlotLetra>();
    readonly List<LetraArrastavel> letras = new List<LetraArrastavel>();

    List<PalavraData> sessao;
    PalavraData palavraAtual;
    int indice;
    int errosNaPalavra;
    int errosTotais;
    bool bloqueado;

    // Identifica esta fase no save: minijogo + mundo.
    string IdFase
    {
        get { return "MonteAPalavra_" + GameManager.Instance.mundoAtual; }
    }

    void Start()
    {
        IniciarSessao();
    }

    // ---------- Fluxo ----------

    public void IniciarSessao()
    {
        StopAllCoroutines();
        telaResultado.gameObject.SetActive(false);

        var gm = GameManager.Instance;
        var disponiveis = gm.PalavrasDoMundo(gm.mundoAtual);

        if (disponiveis.Count == 0)
        {
            Debug.LogError("Nenhuma PalavraData encontrada para o mundo " + gm.mundoAtual);
            return;
        }

        int quantidade = gm.PalavrasPorFase;
        if (disponiveis.Count < quantidade)
        {
            Debug.LogWarning("O mundo " + gm.mundoAtual + " tem só " + disponiveis.Count +
                             " palavras, mas a fase pede " + quantidade + ". A fase usará " + disponiveis.Count + ".");
            quantidade = disponiveis.Count;
        }

        Utilidades.Embaralhar(disponiveis);
        sessao = disponiveis.GetRange(0, quantidade);
        indice = 0;
        errosTotais = 0;

        MostrarPalavra();
    }

    void MostrarPalavra()
    {
        LimparPalavra();

        palavraAtual = sessao[indice];
        errosNaPalavra = 0;
        bloqueado = false;
        IniciarTimer();

        textoProgresso.text = (indice + 1) + " / " + sessao.Count;

        imagemPalavra.sprite = palavraAtual.imagem;
        imagemPalavra.enabled = palavraAtual.imagem != null;
        imagemPalavra.transform.DOKill();
        imagemPalavra.transform.localScale = Vector3.one * 0.7f;
        imagemPalavra.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        textoProgresso.transform.DOKill(true);
        textoProgresso.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f);

        string texto = palavraAtual.texto;
        bool minuscula = GameManager.Instance.Progresso.letraMinuscula;

        // Espaços da palavra
        foreach (char c in texto)
        {
            var slot = Instantiate(prefabSlot, containerSlots);
            slot.Configurar(c, this);
            slots.Add(slot);
        }

        // Bandeja: letras da palavra + distratoras, embaralhadas
        var bandeja = new List<char>(texto);
        for (int i = 0; i < letrasExtras; i++)
            bandeja.Add(LetraExtra(bandeja));
        Utilidades.Embaralhar(bandeja);

        Vector2 tamanho = ((RectTransform)prefabLetra.transform).sizeDelta;

        foreach (char c in bandeja)
        {
            // Cada letra tem um "bolso" fixo: quando ela sai para ser arrastada,
            // as outras não mudam de lugar.
            var bolso = new GameObject("Bolso", typeof(RectTransform), typeof(LayoutElement));
            bolso.transform.SetParent(containerBandeja, false);

            var elemento = bolso.GetComponent<LayoutElement>();
            elemento.preferredWidth = tamanho.x;
            elemento.preferredHeight = tamanho.y;

            var letra = Instantiate(prefabLetra, bolso.transform);
            letra.Configurar(c, bolso.transform, minuscula);
            letras.Add(letra);
        }

        OuvirPalavra();
    }

    void LimparPalavra()
    {
        slots.Clear();
        letras.Clear();

        foreach (Transform filho in containerSlots)
        {
            filho.gameObject.SetActive(false); // sai do layout imediatamente
            Destroy(filho.gameObject);
        }
        foreach (Transform filho in containerBandeja)
        {
            filho.gameObject.SetActive(false);
            Destroy(filho.gameObject);
        }
    }

    // ---------- Chamado pelo SlotLetra ----------

    public void TentarEncaixar(LetraArrastavel letra, SlotLetra slot)
    {
        if (bloqueado || slot.Preenchido) return;

        if (letra.Letra == slot.LetraEsperada)
        {
            slot.Preencher();
            letra.Encaixar(slot.transform);
            slot.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
            SomManager.Tocar(SomId.Acerto);

            if (slots.TrueForAll(s => s.Preenchido))
                StartCoroutine(CompletarPalavra());
        }
        else
        {
            letra.MarcarErro();
            errosNaPalavra++;
            errosTotais++;
            GameManager.Instance.RegistrarResultado(palavraAtual, false);
            SomManager.Tocar(SomId.Erro);

            if (errosNaPalavra >= errosParaDica)
                MostrarDica();
        }
    }

    void MostrarDica()
    {
        SlotLetra alvo = slots.Find(s => !s.Preenchido);
        if (alvo == null) return;

        LetraArrastavel dica = letras.Find(l => !l.Travada && l.Letra == alvo.LetraEsperada);
        if (dica == null) return;

        alvo.Destacar();
        dica.Pulsar();
    }

    IEnumerator CompletarPalavra()
    {
        bloqueado = true;
        if (timer != null) timer.Parar();
        bool figurinhaNova = GameManager.Instance.RegistrarResultado(palavraAtual, true);
        if (figurinhaNova && avisoFigurinha != null)
            avisoFigurinha.Mostrar(palavraAtual);
        SomManager.Tocar(SomId.PalavraCompleta);

        for (int i = 0; i < slots.Count; i++)
        {
            var letra = slots[i].GetComponentInChildren<LetraArrastavel>();
            if (letra != null) letra.Comemorar(i * 0.08f);
            ParticulasUI.Faiscas((RectTransform)slots[i].transform, 5);
        }

        float espera = 1f + (palavraAtual.audio != null ? palavraAtual.audio.length : 0f);
        yield return new WaitForSeconds(espera);

        indice++;
        if (indice >= sessao.Count) MostrarFim();
        else MostrarPalavra();
    }

    void MostrarFim()
    {
        if (timer != null) timer.Esconder();

        int estrelas = GameManager.CalcularEstrelas(errosTotais, sessao.Count);
        GameManager.Instance.RegistrarFase(IdFase, estrelas);

        Debug.Log("[Minijogo 1] Fase concluída. Palavras: " + sessao.Count +
                  " | Erros: " + errosTotais + " | Estrelas: " + estrelas);

        SomManager.Tocar(SomId.FimDeFase);
        telaResultado.Mostrar(estrelas);
    }

    // ---------- Timer ----------

    void IniciarTimer()
    {
        if (timer == null) return;

        var gm = GameManager.Instance;
        if (gm.TimerAtivo) timer.Iniciar(gm.SegundosPorPalavra, AoAcabarOTempo);
        else timer.Esconder();
    }

    // Acabou o tempo: conta como um erro e mais nada (o timer é um incentivo, não uma punição).
    void AoAcabarOTempo()
    {
        if (bloqueado) return;

        errosTotais++;
        GameManager.Instance.RegistrarResultado(palavraAtual, false);
    }

    // ---------- Botões ----------

    public void OuvirPalavra()
    {
        if (palavraAtual != null)
            Tocar(palavraAtual.audio);
    }

    public void JogarDeNovo()
    {
        IniciarSessao();
    }

    public void VoltarParaTrilha()
    {
        SceneManager.LoadScene("Trilha");
    }

    // ---------- Utilitários ----------

    void Tocar(AudioClip clip)
    {
        if (clip == null || !GameManager.Instance.Progresso.somLigado || audioSource.isPlaying) return;
        audioSource.PlayOneShot(clip);
    }

    static char LetraExtra(List<char> jaNaBandeja)
    {
        for (int tentativas = 0; tentativas < 50; tentativas++)
        {
            char c = Alfabeto[Random.Range(0, Alfabeto.Length)];
            if (!jaNaBandeja.Contains(c)) return c;
        }
        return 'X';
    }
}
