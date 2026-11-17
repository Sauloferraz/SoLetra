using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Minijogo 2: a criança lê a palavra e toca na imagem certa entre 3 opções.
// O botão OUVIR é uma ajuda opcional.
public class QualEAPalavraController : MonoBehaviour
{
    const int QuantidadeOpcoes = 3;

    [Header("Referências de cena")]
    [SerializeField] TMP_Text textoPalavra;
    [SerializeField] RectTransform containerOpcoes;
    [SerializeField] TMP_Text textoProgresso;
    [SerializeField] TelaResultado telaResultado;
    [SerializeField] AudioSource audioSource;
    [SerializeField] TimerPalavra timer;
    [SerializeField] AvisoFigurinha avisoFigurinha;

    [Header("Prefab")]
    [SerializeField] OpcaoPalavra prefabOpcao;

    List<PalavraData> sessao;
    PalavraData palavraAtual;
    int indice;
    int errosTotais;
    bool bloqueado;

    // Identifica esta fase no save: minijogo + mundo (o prefixo é o mesmo usado pelo BotaoFase da Trilha).
    string IdFase
    {
        get { return "QualEAPalavra_" + GameManager.Instance.mundoAtual; }
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
        LimparOpcoes();

        palavraAtual = sessao[indice];
        bloqueado = false;
        IniciarTimer();

        textoProgresso.text = (indice + 1) + " / " + sessao.Count;

        bool minuscula = GameManager.Instance.Progresso.letraMinuscula;
        textoPalavra.text = minuscula ? palavraAtual.texto.ToLowerInvariant() : palavraAtual.texto;
        textoPalavra.transform.DOKill();
        textoPalavra.transform.localScale = Vector3.one * 0.7f;
        textoPalavra.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        textoProgresso.transform.DOKill(true);
        textoProgresso.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f);

        foreach (var palavra in SortearOpcoes(palavraAtual))
        {
            var opcao = Instantiate(prefabOpcao, containerOpcoes);
            opcao.Configurar(palavra, this);
        }
        
        OuvirPalavra();
    }

    // A palavra certa + 2 do mesmo mundo, embaralhadas.
    List<PalavraData> SortearOpcoes(PalavraData correta)
    {
        var gm = GameManager.Instance;

        var candidatas = gm.PalavrasDoMundo(gm.mundoAtual);
        candidatas.Remove(correta);

        // Mundo com poucas palavras: completa com as de outros mundos.
        if (candidatas.Count < QuantidadeOpcoes - 1)
        {
            foreach (var palavra in gm.TodasPalavras)
            {
                if (palavra != correta && !candidatas.Contains(palavra))
                    candidatas.Add(palavra);
            }
        }

        Utilidades.Embaralhar(candidatas);

        var opcoes = new List<PalavraData> { correta };
        opcoes.AddRange(candidatas.GetRange(0, Mathf.Min(QuantidadeOpcoes - 1, candidatas.Count)));
        Utilidades.Embaralhar(opcoes);
        return opcoes;
    }

    void LimparOpcoes()
    {
        foreach (Transform filho in containerOpcoes)
        {
            filho.gameObject.SetActive(false); // sai do layout imediatamente
            Destroy(filho.gameObject);
        }
    }

    // ---------- Chamado pela OpcaoPalavra ----------

    public void Escolher(OpcaoPalavra opcao)
    {
        if (bloqueado) return;

        if (opcao.Palavra == palavraAtual)
        {
            bloqueado = true;
            if (timer != null) timer.Parar();
            opcao.MostrarAcerto();
            ParticulasUI.Faiscas((RectTransform)opcao.transform, 14);
            StartCoroutine(CompletarPalavra());
        }
        else
        {
            errosTotais++;
            GameManager.Instance.RegistrarResultado(palavraAtual, false);
            opcao.MostrarErro();
            SomManager.Tocar(SomId.Erro);
        }
    }

    IEnumerator CompletarPalavra()
    {
        bool figurinhaNova = GameManager.Instance.RegistrarResultado(palavraAtual, true);
        if (figurinhaNova && avisoFigurinha != null)
            avisoFigurinha.Mostrar(palavraAtual);
        SomManager.Tocar(SomId.Acerto);

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

        Debug.Log("[Minijogo 2] Fase concluída. Palavras: " + sessao.Count +
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
}
