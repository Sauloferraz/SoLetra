using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Minijogo 3: a criança vê a imagem, ouve a palavra e arrasta as sílabas para formar a palavra.
// As sílabas só encaixam NA ORDEM, da esquerda para a direita: o próximo espaço fica destacado.
public class JunteAsSilabasController : MonoBehaviour
{
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
    [SerializeField] SlotSilaba prefabSlot;
    [SerializeField] SilabaArrastavel prefabSilaba;

    [Header("Configuração")]
    [SerializeField] int silabasExtras = 2;
    [SerializeField] int errosParaDica = 2;

    readonly List<SlotSilaba> slots = new List<SlotSilaba>();
    readonly List<SilabaArrastavel> silabas = new List<SilabaArrastavel>();

    List<PalavraData> sessao;
    PalavraData palavraAtual;
    int indice;
    int proximo;            // índice do próximo espaço a preencher
    int errosNaPalavra;
    int errosTotais;
    bool bloqueado;

    // Identifica esta fase no save: minijogo + mundo (o prefixo é o mesmo usado pelo BotaoFase da Trilha).
    string IdFase
    {
        get { return "JunteAsSilabas_" + GameManager.Instance.mundoAtual; }
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

        // Só palavras com pelo menos 2 sílabas cadastradas servem para este minijogo.
        disponiveis.RemoveAll(p => p.silabas == null || p.silabas.Length < 2);

        if (disponiveis.Count == 0)
        {
            Debug.LogError("Nenhuma palavra com sílabas encontrada para o mundo " + gm.mundoAtual);
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
        proximo = 0;
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

        bool minuscula = GameManager.Instance.Progresso.letraMinuscula;

        // Espaços: um por sílaba
        foreach (string silaba in palavraAtual.silabas)
        {
            var slot = Instantiate(prefabSlot, containerSlots);
            slot.Configurar(silaba, this);
            slots.Add(slot);
        }
        AtualizarSlotAtivo();

        // Bandeja: sílabas da palavra (inclusive repetidas, como NA-NA) + distratoras, embaralhadas
        var bandeja = new List<string>(palavraAtual.silabas);
        bandeja.AddRange(SortearExtras(palavraAtual));
        Utilidades.Embaralhar(bandeja);

        Vector2 tamanho = ((RectTransform)prefabSilaba.transform).sizeDelta;

        foreach (string silaba in bandeja)
        {
            // Cada sílaba tem um "bolso" fixo: quando ela sai para ser arrastada,
            // as outras não mudam de lugar.
            var bolso = new GameObject("Bolso", typeof(RectTransform), typeof(LayoutElement));
            bolso.transform.SetParent(containerBandeja, false);

            var elemento = bolso.GetComponent<LayoutElement>();
            elemento.preferredWidth = tamanho.x;
            elemento.preferredHeight = tamanho.y;

            var item = Instantiate(prefabSilaba, bolso.transform);
            item.Configurar(silaba, bolso.transform, minuscula);
            silabas.Add(item);
        }

        OuvirPalavra();
    }

    // Sílabas de outras palavras do mesmo mundo que não aparecem na palavra atual.
    List<string> SortearExtras(PalavraData atual)
    {
        var gm = GameManager.Instance;
        var daPalavra = new HashSet<string>(atual.silabas);
        var candidatas = new List<string>();

        foreach (var palavra in gm.PalavrasDoMundo(gm.mundoAtual))
        {
            if (palavra == atual || palavra.silabas == null) continue;

            foreach (string silaba in palavra.silabas)
            {
                if (!daPalavra.Contains(silaba) && !candidatas.Contains(silaba))
                    candidatas.Add(silaba);
            }
        }

        Utilidades.Embaralhar(candidatas);
        return candidatas.GetRange(0, Mathf.Min(silabasExtras, candidatas.Count));
    }

    void LimparPalavra()
    {
        slots.Clear();
        silabas.Clear();

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

    // Só o próximo espaço da ordem fica destacado.
    void AtualizarSlotAtivo()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].Marcar(i == proximo);
    }

    // ---------- Chamado pelo SlotSilaba ----------

    public void TentarEncaixar(SilabaArrastavel silaba, SlotSilaba slot)
    {
        if (bloqueado || slot.Preenchido) return;

        bool espacoCerto = slots.IndexOf(slot) == proximo;

        if (espacoCerto && silaba.Silaba == slot.SilabaEsperada)
        {
            slot.Preencher();
            silaba.Encaixar(slot.transform);
            slot.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
            SomManager.Tocar(SomId.Acerto);

            proximo++;
            if (proximo >= slots.Count)
                StartCoroutine(CompletarPalavra());
            else
                AtualizarSlotAtivo();
        }
        else
        {
            // Sílaba errada, ou sílaba certa mas fora da ordem.
            silaba.MarcarErro();
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
        if (proximo >= slots.Count) return;

        SlotSilaba alvo = slots[proximo];
        SilabaArrastavel dica = silabas.Find(s => !s.Travada && s.Silaba == alvo.SilabaEsperada);
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
            var silaba = slots[i].GetComponentInChildren<SilabaArrastavel>();
            if (silaba != null) silaba.Comemorar(i * 0.12f);
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

        Debug.Log("[Minijogo 3] Fase concluída. Palavras: " + sessao.Count +
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
