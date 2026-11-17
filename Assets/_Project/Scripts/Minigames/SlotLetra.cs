using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Espaço onde uma letra da palavra deve ser encaixada.
public class SlotLetra : MonoBehaviour, IDropHandler
{
    public char LetraEsperada { get; private set; }
    public bool Preenchido { get; private set; }

    readonly Color corDestaque = new Color32(0xFF, 0xD1, 0x66, 0xFF);

    MonteAPalavraController controlador;
    Image fundo;
    Color corNormal;

    void Awake()
    {
        fundo = GetComponent<Image>();
        corNormal = fundo.color;
    }

    public void Configurar(char letra, MonteAPalavraController dono)
    {
        LetraEsperada = letra;
        controlador = dono;
        Preenchido = false;
    }

    public void Preencher()
    {
        Preenchido = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var letra = eventData.pointerDrag.GetComponent<LetraArrastavel>();
        if (letra != null)
            controlador.TentarEncaixar(letra, this);
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
            fundo.color = corDestaque;
            yield return new WaitForSeconds(0.25f);
            fundo.color = corNormal;
            yield return new WaitForSeconds(0.25f);
        }
    }
}
