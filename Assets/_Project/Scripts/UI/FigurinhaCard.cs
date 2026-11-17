using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Uma figurinha do álbum, em formato de carta colecionável.
// A cor da moldura e da faixa depende do mundo (verde, azul, roxo).
// Ainda não ganha: carta "virada" (escura, com "?"), mas com o número da coleção.
// Toque rápido: fala a palavra. Segurar: abre o zoom da carta; soltar: o zoom some.
public class FigurinhaCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Button botao;
    [SerializeField] Image borda;
    [SerializeField] Image miolo;
    [SerializeField] Image janela;
    [SerializeField] Image imagem;
    [SerializeField] Image faixa;
    [SerializeField] TMP_Text nome;
    [SerializeField] TMP_Text numero;
    [SerializeField] TMP_Text interrogacao;
    [SerializeField] GameObject badgeNova;

    [Header("Zoom ao segurar")]
    [SerializeField] float tempoParaZoom = 0.25f;
    [SerializeField] float tolerancia = 20f; // quanto o dedo pode andar (px) antes de virar rolagem

    static readonly Color[] CoresMundo =
    {
        new Color32(0x2F, 0xBF, 0x71, 0xFF), // mundo 1: verde
        new Color32(0x3D, 0x8B, 0xFD, 0xFF), // mundo 2: azul
        new Color32(0x9B, 0x5D, 0xE5, 0xFF), // mundo 3: roxo
    };

    readonly Color corMiolo = new Color32(0xFF, 0xF8, 0xE7, 0xFF);
    readonly Color corMioloTravado = new Color32(0x3A, 0x3D, 0x57, 0xFF);
    readonly Color corBordaTravada = new Color32(0x8D, 0x8F, 0xA3, 0xFF);
    readonly Color corJanelaTravada = new Color32(0x4A, 0x4D, 0x6A, 0xFF);

    PalavraData palavra;
    AlbumController album;
    RectTransform rect;
    int numeroDaCarta;
    bool usaMinuscula;
    bool desbloqueada;

    // Estado do "segurar"
    PointerEventData toque;
    Vector2 posicaoInicial;
    bool zoomAberto;
    bool segurouParaZoom;
    Coroutine segurando;

    public void Configurar(PalavraData dados, int posicao, bool ganha, bool nova, bool minuscula, AlbumController dono)
    {
        palavra = dados;
        album = dono;
        desbloqueada = ganha;
        numeroDaCarta = posicao;
        usaMinuscula = minuscula;
        rect = (RectTransform)transform;

        numero.text = "#" + posicao.ToString("00");

        Color corMundo = CoresMundo[Mathf.Clamp(dados.mundo - 1, 0, CoresMundo.Length - 1)];

        if (ganha)
        {
            borda.color = corMundo;
            miolo.color = corMiolo;
            janela.color = Color.white;
            faixa.color = corMundo;
            faixa.gameObject.SetActive(true);

            imagem.sprite = dados.imagem;
            imagem.enabled = dados.imagem != null;

            interrogacao.gameObject.SetActive(false);
            nome.text = minuscula ? dados.texto.ToLowerInvariant() : dados.texto;
        }
        else
        {
            borda.color = corBordaTravada;
            miolo.color = corMioloTravado;
            janela.color = corJanelaTravada;
            faixa.gameObject.SetActive(false);

            imagem.enabled = false;
            interrogacao.gameObject.SetActive(true);
        }

        badgeNova.SetActive(ganha && nova);
        if (ganha && nova) StartCoroutine(PulsarSelo());

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(AoClicar);
    }

    // Cópia usada pelo zoom: sempre mostra a carta ganha e não reage a toques.
    public void ConfigurarParaZoom(PalavraData dados, int posicao, bool minuscula)
    {
        Configurar(dados, posicao, true, false, minuscula, null);
    }

    // ---------- Toque rápido ----------

    void AoClicar()
    {
        // Se o clique veio de soltar depois de um zoom, não fala a palavra de novo.
        if (segurouParaZoom)
        {
            segurouParaZoom = false;
            return;
        }

        if (!desbloqueada || album == null) return;

        album.TocarPalavra(palavra);
        StartCoroutine(Pular());
    }

    IEnumerator Pular()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    // ---------- Segurar para dar zoom ----------

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!desbloqueada || album == null) return;

        segurouParaZoom = false;
        toque = eventData;
        posicaoInicial = eventData.position;

        if (segurando != null) StopCoroutine(segurando);
        segurando = StartCoroutine(Segurar());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Soltar();
    }

    void OnDisable()
    {
        Soltar();
    }

    IEnumerator Segurar()
    {
        // Espera o tempo de "segurar". Se o dedo andar ou virar rolagem, cancela.
        float t = 0f;
        while (t < tempoParaZoom)
        {
            if (ViraRolagem(false)) { segurando = null; yield break; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        zoomAberto = true;
        segurouParaZoom = true;
        album.MostrarZoom(palavra, numeroDaCarta, usaMinuscula);

        // Com o zoom aberto, só uma rolagem de verdade (arrasto) o fecha.
        while (zoomAberto)
        {
            if (ViraRolagem(true)) Soltar();
            yield return null;
        }
    }

    bool ViraRolagem(bool somenteArrasto)
    {
        if (toque == null) return false;
        if (toque.dragging) return true;
        if (somenteArrasto) return false;
        return (toque.position - posicaoInicial).sqrMagnitude > tolerancia * tolerancia;
    }

    void Soltar()
    {
        if (segurando != null)
        {
            StopCoroutine(segurando);
            segurando = null;
        }

        if (zoomAberto)
        {
            zoomAberto = false;
            if (album != null) album.EsconderZoom();
        }

        toque = null;
    }

    // O selo "NOVA!" fica pulsando devagar.
    IEnumerator PulsarSelo()
    {
        var seloRect = (RectTransform)badgeNova.transform;
        while (true)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
            {
                float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
                seloRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
    }
}
