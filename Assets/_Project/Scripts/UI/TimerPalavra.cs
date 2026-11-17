using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de tempo de uma palavra, bem visível: barra grande + número de segundos ao lado.
// - Esvazia da direita para a esquerda e muda de cor (turquesa, amarelo, coral).
// - Nos últimos segundos, pulsa a cada segundo e toca um "tique".
// - Ao zerar: aviso sonoro, tremor leve da tela e a barra pisca em vermelho.
// O timer é um incentivo, não uma punição: o que conta como consequência quem decide é o controlador.
public class TimerPalavra : MonoBehaviour
{
    [SerializeField] Image fundo;
    [SerializeField] Image preenchimento;
    [SerializeField] TMP_Text texto;
    [SerializeField] RectTransform conteudoParaTremer;

    [Header("Efeitos")]
    [SerializeField] int segundosDeAlerta = 5;
    [SerializeField] float forcaDoTremor = 14f;
    [SerializeField] float duracaoDoTremor = 0.4f;

    readonly Color corCheia = new Color32(0x1B, 0x99, 0x8B, 0xFF);
    readonly Color corMeio = new Color32(0xFF, 0xD1, 0x66, 0xFF);
    readonly Color corVazia = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
    readonly Color corTexto = new Color32(0x2D, 0x30, 0x47, 0xFF);

    System.Action aoAcabar;
    Color corFundoOriginal;
    Vector2 posicaoOriginalDoConteudo;
    float duracao;
    float restante;
    int ultimoSegundo;
    bool rodando;

    void Awake()
    {
        corFundoOriginal = fundo.color;
        if (conteudoParaTremer != null)
            posicaoOriginalDoConteudo = conteudoParaTremer.anchoredPosition;
    }

    void OnDestroy()
    {
        MatarEfeitos();
    }

    public void Iniciar(float segundos, System.Action quandoAcabar)
    {
        gameObject.SetActive(true);
        MatarEfeitos();
        RestaurarVisual();

        duracao = Mathf.Max(1f, segundos);
        restante = duracao;
        aoAcabar = quandoAcabar;
        rodando = true;

        ultimoSegundo = Mathf.CeilToInt(restante);
        texto.text = ultimoSegundo.ToString();
        AtualizarBarra();

        // Entra com um "pop" para chamar a atenção a cada palavra nova.
        transform.localScale = Vector3.one * 0.8f;
        transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
    }

    // Congela a barra (palavra concluída).
    public void Parar()
    {
        rodando = false;
    }

    // Esconde a barra (timer desligado ou fim da fase).
    public void Esconder()
    {
        rodando = false;
        MatarEfeitos();
        RestaurarVisual();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!rodando) return;

        restante -= Time.deltaTime;

        if (restante <= 0f)
        {
            restante = 0f;
            rodando = false;
            AtualizarBarra();
            AoZerar();
            return;
        }

        int segundo = Mathf.CeilToInt(restante);
        if (segundo != ultimoSegundo)
        {
            ultimoSegundo = segundo;
            texto.text = segundo.ToString();
            if (segundo <= segundosDeAlerta) Alerta();
        }

        AtualizarBarra();
    }

    void AtualizarBarra()
    {
        float fracao = restante / duracao;
        preenchimento.fillAmount = fracao;
        preenchimento.color = fracao > 0.5f
            ? Color.Lerp(corMeio, corCheia, (fracao - 0.5f) * 2f)
            : Color.Lerp(corVazia, corMeio, fracao * 2f);

        texto.color = restante <= segundosDeAlerta ? corVazia : corTexto;
    }

    // Últimos segundos: tique + pulso da barra e do número.
    void Alerta()
    {
        SomManager.Tocar(SomId.TimerTique);

        transform.DOKill(true);
        texto.transform.DOKill(true);

        transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6, 0.8f);
        texto.transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 6, 0.8f);
    }

    void AoZerar()
    {
        texto.text = "0";

        SomManager.Tocar(SomId.TimerAcabou);
        TremerTela();
        PiscarVermelho();

        if (aoAcabar != null) aoAcabar();
    }

    // Tremor leve do contêiner de toda a tela do minijogo.
    void TremerTela()
    {
        if (conteudoParaTremer == null) return;

        conteudoParaTremer.DOKill();
        conteudoParaTremer.anchoredPosition = posicaoOriginalDoConteudo;
        conteudoParaTremer
            .DOShakeAnchorPos(duracaoDoTremor, forcaDoTremor, 30, 90f, false, true)
            .OnComplete(() => conteudoParaTremer.anchoredPosition = posicaoOriginalDoConteudo);
    }

    // O fundo da barra pisca em vermelho (3 vezes) e volta ao normal.
    void PiscarVermelho()
    {
        fundo.DOKill();
        fundo.color = corFundoOriginal;
        fundo.DOColor(new Color(corVazia.r, corVazia.g, corVazia.b, 0.85f), 0.12f).SetLoops(6, LoopType.Yoyo);

        transform.DOKill(true);
        transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 0.8f);
    }

    void MatarEfeitos()
    {
        transform.DOKill();
        if (texto != null) texto.transform.DOKill();
        if (fundo != null) fundo.DOKill();

        if (conteudoParaTremer != null)
        {
            conteudoParaTremer.DOKill();
            conteudoParaTremer.anchoredPosition = posicaoOriginalDoConteudo;
        }
    }

    void RestaurarVisual()
    {
        transform.localScale = Vector3.one;
        if (texto != null) texto.transform.localScale = Vector3.one;
        if (fundo != null) fundo.color = corFundoOriginal;
    }
}
