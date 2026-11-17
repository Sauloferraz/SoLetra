using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Uma das 3 imagens que a criança pode tocar no minijogo "Qual é a Palavra?".
[RequireComponent(typeof(CanvasGroup))]
public class OpcaoPalavra : MonoBehaviour
{
    [SerializeField] Button botao;
    [SerializeField] Image fundo;
    [SerializeField] Image imagem;
    [SerializeField] TMP_Text textoReserva; // aparece só se a palavra ainda não tem imagem

    readonly Color corErro = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
    readonly Color corAcerto = new Color32(0x7B, 0xD3, 0x89, 0xFF);

    public PalavraData Palavra { get; private set; }

    QualEAPalavraController controlador;
    RectTransform rect;
    CanvasGroup grupo;
    Color corNormal;

    void Awake()
    {
        rect = (RectTransform)transform;
        grupo = GetComponent<CanvasGroup>();
        corNormal = fundo.color;
        botao.onClick.AddListener(AoClicar);
    }

    public void Configurar(PalavraData palavra, QualEAPalavraController dono)
    {
        Palavra = palavra;
        controlador = dono;

        bool temImagem = palavra.imagem != null;
        imagem.sprite = palavra.imagem;
        imagem.enabled = temImagem;

        textoReserva.gameObject.SetActive(!temImagem);
        textoReserva.text = palavra.texto;
    }

    void AoClicar()
    {
        controlador.Escolher(this);
    }

    // Errou: balança, fica avermelhada e esmaece. Não pode mais ser tocada.
    public void MostrarErro()
    {
        StartCoroutine(ErroRotina());
    }

    IEnumerator ErroRotina()
    {
        grupo.blocksRaycasts = false;
        fundo.color = corErro;

        // Balança girando (rotação não briga com o LayoutGroup).
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            float angulo = Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t);
            rect.localRotation = Quaternion.Euler(0f, 0f, angulo);
            yield return null;
        }
        rect.localRotation = Quaternion.identity;
        fundo.color = corNormal;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            grupo.alpha = Mathf.Lerp(1f, 0.25f, t);
            yield return null;
        }
        grupo.alpha = 0.25f;
    }

    // Acertou: fica verde e dá um pulinho.
    public void MostrarAcerto()
    {
        fundo.color = corAcerto;
        StartCoroutine(AcertoRotina());
    }

    IEnumerator AcertoRotina()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            float s = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }
}
