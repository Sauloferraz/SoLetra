using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Letra da bandeja que a criança arrasta até um SlotLetra.
// Funciona igual com mouse (Windows) e toque (Android), via EventSystem + Input System UI Module.
[RequireComponent(typeof(CanvasGroup))]
public class LetraArrastavel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] TMP_Text texto;
    [SerializeField] Image fundo;

    readonly Color corErro = new Color32(0xFF, 0x6B, 0x6B, 0xFF);

    public char Letra { get; private set; }
    public bool Travada { get; private set; }

    RectTransform rect;
    CanvasGroup grupo;
    Canvas canvasRaiz;
    Transform bolso;        // posição de origem na bandeja (fixa, não desloca as outras letras)
    Color corNormal;
    bool erroNoSoltar;

    void Awake()
    {
        rect = (RectTransform)transform;
        grupo = GetComponent<CanvasGroup>();
        corNormal = fundo.color;
    }

    public void Configurar(char letra, Transform bolsoOrigem, bool minuscula)
    {
        Letra = letra;
        bolso = bolsoOrigem;
        canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
        texto.text = minuscula ? letra.ToString().ToLowerInvariant() : letra.ToString();
    }

    // Chamado pelo controlador quando o encaixe estava certo.
    public void Encaixar(Transform slot)
    {
        Travada = true;
        grupo.blocksRaycasts = false;

        transform.SetParent(slot, false);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    // Chamado pelo controlador quando a letra foi solta no slot errado.
    public void MarcarErro()
    {
        erroNoSoltar = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Travada) return;

        StopAllCoroutines();
        fundo.color = corNormal;

        transform.SetParent(canvasRaiz.transform, true);
        transform.SetAsLastSibling();
        rect.localScale = Vector3.one * 1.1f;
        grupo.blocksRaycasts = false; // deixa o slot embaixo receber o OnDrop
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Travada) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Travada) return;

        grupo.blocksRaycasts = true;
        StartCoroutine(Voltar(erroNoSoltar));
        erroNoSoltar = false;
    }

    IEnumerator Voltar(bool tremer)
    {
        transform.SetParent(bolso, true);
        rect.localScale = Vector3.one;

        Vector2 inicio = rect.anchoredPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
        {
            rect.anchoredPosition = Vector2.Lerp(inicio, Vector2.zero, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero;

        if (tremer)
            yield return Tremer();
    }

    IEnumerator Tremer()
    {
        fundo.color = corErro;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            float x = Mathf.Sin(t * Mathf.PI * 6f) * 15f * (1f - t);
            rect.anchoredPosition = new Vector2(x, 0f);
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero;
        fundo.color = corNormal;
    }

    // Dica: a letra "pulsa" na bandeja.
    public void Pulsar()
    {
        StartCoroutine(PulsarRotina());
    }

    IEnumerator PulsarRotina()
    {
        for (int i = 0; i < 3; i++)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
            {
                float s = 1f + 0.25f * Mathf.Sin(t * Mathf.PI);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
        rect.localScale = Vector3.one;
    }

    // Palavra completa: cada letra dá um pulinho, em sequência.
    public void Comemorar(float atraso)
    {
        StartCoroutine(ComemorarRotina(atraso));
    }

    IEnumerator ComemorarRotina(float atraso)
    {
        yield return new WaitForSeconds(atraso);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            float s = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }
}
