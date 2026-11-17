using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Espaço onde uma sílaba da palavra deve ser encaixada.
// O espaço "ativo" (o próximo da ordem, da esquerda para a direita) fica destacado.
public class SlotSilaba : MonoBehaviour, IDropHandler
{
    public string SilabaEsperada { get; private set; }
    public bool Preenchido { get; private set; }

    readonly Color corAtivo = new Color32(0xFF, 0xE9, 0xB0, 0xFF);
    readonly Color corPisca = new Color32(0xFF, 0xD1, 0x66, 0xFF);

    JunteAsSilabasController controlador;
    Image fundo;
    Color corNormal;
    bool ativo;

    void Awake()
    {
        fundo = GetComponent<Image>();
        corNormal = fundo.color;
    }

    public void Configurar(string silaba, JunteAsSilabasController dono)
    {
        SilabaEsperada = silaba;
        controlador = dono;
        Preenchido = false;
    }

    public void Preencher()
    {
        Preenchido = true;
        Marcar(false);
    }

    // Marca (ou desmarca) este espaço como o próximo a ser preenchido.
    public void Marcar(bool marcar)
    {
        ativo = marcar;
        fundo.color = ativo ? corAtivo : corNormal;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var silaba = eventData.pointerDrag.GetComponent<SilabaArrastavel>();
        if (silaba != null)
            controlador.TentarEncaixar(silaba, this);
    }

    public void Destacar()
    {
        StopAllCoroutines();
        StartCoroutine(Piscar());
    }

    IEnumerator Piscar()
    {
        for (int i = 0; i < 3; i++)
        {
            fundo.color = corPisca;
            yield return new WaitForSeconds(0.25f);
            fundo.color = ativo ? corAtivo : corNormal;
            yield return new WaitForSeconds(0.25f);
        }
    }
}
