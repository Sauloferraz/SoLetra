using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela de fim de fase: título + estrelas animadas.
// Será usada pelos 3 minijogos, por isso fica separada dos controladores.
public class TelaResultado : MonoBehaviour
{
    [SerializeField] TMP_Text titulo;
    [SerializeField] Image[] estrelas;

    readonly Color corGanha = new Color32(0xFF, 0xD1, 0x66, 0xFF);
    readonly Color corApagada = new Color32(0x6B, 0x6E, 0x85, 0xFF);

    public void Mostrar(int quantidade)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        titulo.text = quantidade >= 3 ? "PERFEITO!"
                    : quantidade == 2 ? "MUITO BEM!"
                    : "BOM TRABALHO!";

        titulo.transform.DOKill();
        titulo.transform.localScale = Vector3.zero;
        titulo.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);

        StartCoroutine(AnimarEstrelas(quantidade));
    }

    IEnumerator AnimarEstrelas(int quantidade)
    {
        foreach (var estrela in estrelas)
        {
            estrela.color = corApagada;
            estrela.rectTransform.localScale = Vector3.zero;
        }

        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < estrelas.Length; i++)
        {
            bool ganha = i < quantidade;
            estrelas[i].color = ganha ? corGanha : corApagada;

            if (ganha)
            {
                SomManager.Tocar(SomId.Estrela);
                ParticulasUI.Faiscas(estrelas[i].rectTransform, 10);
            }

            yield return Surgir(estrelas[i].rectTransform, ganha ? 1f : 0.8f);
            yield return new WaitForSeconds(ganha ? 0.15f : 0.05f);
        }

        if (quantidade >= 3)
        {
            ParticulasUI.ChuvaDeConfete(70, 1.6f);
            ParticulasUI.Confete(titulo.rectTransform, 30);
        }
    }

    // Aparece crescendo, passando um pouco do tamanho final e acomodando ("ease out back").
    IEnumerator Surgir(RectTransform alvo, float escalaFinal)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            float x = t - 1f;
            float curva = 1f + c3 * x * x * x + c1 * x * x;
            float s = escalaFinal * curva;
            alvo.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        alvo.localScale = new Vector3(escalaFinal, escalaFinal, 1f);
    }
}
