using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Zoom de uma figurinha do álbum: fundo escurecido, carta grande no centro, brilho girando atrás,
// carta "pairando" e faíscas. Aparece ao segurar uma figurinha e some ao soltar.
public class ZoomFigurina : MonoBehaviour
{
    [SerializeField] CanvasGroup grupo;
    [SerializeField] RectTransform areaCarta;
    [SerializeField] Image brilho;
    [SerializeField] FigurinhaCard prefabCard;
    [SerializeField] float escalaFinal = 3f;

    FigurinhaCard carta;

    void OnDisable()
    {
        DOTween.Kill(this);
    }

    public void Mostrar(PalavraData palavra, int posicao, bool minuscula)
    {
        DOTween.Kill(this);
        gameObject.SetActive(true);

        if (carta == null)
            carta = Instantiate(prefabCard, areaCarta);
        carta.ConfigurarParaZoom(palavra, posicao, minuscula);

        var cartaRect = (RectTransform)carta.transform;
        cartaRect.anchoredPosition = Vector2.zero;
        cartaRect.localScale = Vector3.one * 1.2f;
        cartaRect.localRotation = Quaternion.Euler(0f, 0f, -10f);
        areaCarta.localScale = Vector3.one;

        // Fundo escurece
        grupo.alpha = 0f;
        grupo.DOFade(1f, 0.2f).SetId(this);

        // A carta cresce passando um pouco do tamanho final e acomoda, desentortando
        cartaRect.DOScale(escalaFinal, 0.4f).SetEase(Ease.OutBack).SetId(this);
        cartaRect.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.OutBack).SetId(this);

        // Brilho atrás: abre e gira devagar sem parar
        var brilhoRect = brilho.rectTransform;
        brilhoRect.localScale = Vector3.one * 0.5f;
        brilhoRect.localRotation = Quaternion.identity;
        brilhoRect.DOScale(1f, 0.5f).SetEase(Ease.OutCubic).SetId(this);
        brilhoRect.DORotate(new Vector3(0f, 0f, 360f), 14f, RotateMode.FastBeyond360)
                  .SetEase(Ease.Linear).SetLoops(-1).SetId(this);

        // Depois do "pop", a carta paira suavemente
        DOVirtual.DelayedCall(0.4f, () =>
        {
            cartaRect.DOAnchorPosY(14f, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(this);
        }).SetId(this);

        ParticulasUI.Faiscas(areaCarta, 18);
        SomManager.Tocar(SomId.Estrela);
    }

    public void Esconder()
    {
        if (!gameObject.activeSelf) return;

        DOTween.Kill(this);

        DOTween.Sequence().SetId(this)
            .Append(grupo.DOFade(0f, 0.18f))
            .Join(areaCarta.DOScale(0.8f, 0.18f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                areaCarta.localScale = Vector3.one;
                gameObject.SetActive(false);
            });
    }
}
