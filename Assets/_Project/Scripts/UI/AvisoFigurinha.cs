using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Aviso "NOVA FIGURINHA!" que entra pela direita, fica um instante e sai.
public class AvisoFigurinha : MonoBehaviour
{
    [SerializeField] Image imagem;

    RectTransform rect;
    Vector2 posicaoVisivel;
    bool iniciado;

    public void Mostrar(PalavraData palavra)
    {
        if (!iniciado)
        {
            rect = (RectTransform)transform;
            posicaoVisivel = rect.anchoredPosition;
            iniciado = true;
        }

        StopAllCoroutines();

        imagem.sprite = palavra.imagem;
        imagem.enabled = palavra.imagem != null;

        gameObject.SetActive(true);
        SomManager.Tocar(SomId.FigurinhaNova);
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        Vector2 escondida = posicaoVisivel + new Vector2(540f, 0f);

        yield return Mover(escondida, posicaoVisivel, 0.35f);
        ParticulasUI.Faiscas(rect, 12);
        yield return new WaitForSeconds(1.8f);
        yield return Mover(posicaoVisivel, escondida, 0.3f);

        gameObject.SetActive(false);
    }

    IEnumerator Mover(Vector2 de, Vector2 para, float duracao)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracao)
        {
            rect.anchoredPosition = Vector2.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rect.anchoredPosition = para;
    }
}
