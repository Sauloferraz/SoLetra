using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Botão que só dispara depois de ser mantido pressionado (ex.: acesso do professor).
// Uma barra de preenchimento mostra o progresso. Soltar ou sair do botão cancela.
public class BotaoSegurar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] Image preenchimento;
    [SerializeField] float duracao = 3f;

    public UnityEvent aoCompletar;

    bool segurando;
    float tempo;

    public void OnPointerDown(PointerEventData eventData)
    {
        segurando = true;
        tempo = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Cancelar();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cancelar();
    }

    void Update()
    {
        if (!segurando) return;

        tempo += Time.unscaledDeltaTime;
        preenchimento.fillAmount = Mathf.Clamp01(tempo / duracao);

        if (tempo >= duracao)
        {
            Cancelar();
            aoCompletar.Invoke();
        }
    }

    void OnDisable()
    {
        Cancelar();
    }

    void Cancelar()
    {
        segurando = false;
        tempo = 0f;
        if (preenchimento != null) preenchimento.fillAmount = 0f;
    }
}
