using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Painel do professor: palavras por fase, timer, desempenho por palavra e reset do progresso.
// Atualiza tudo toda vez que é aberto (OnEnable).
public class PainelProfessor : MonoBehaviour
{
    [Header("Palavras por fase")]
    [SerializeField] Slider sliderPalavrasPorFase;
    [SerializeField] TMP_Text textoPalavrasPorFase;

    [Header("Timer")]
    [SerializeField] TMP_Text textoTimer;
    [SerializeField] Slider sliderSegundos;
    [SerializeField] TMP_Text textoSegundos;

    [Header("Desempenho e reset")]
    [SerializeField] RectTransform conteudoLista;
    [SerializeField] LinhaDesempenho prefabLinha;
    [SerializeField] GameObject textoVazio;
    [SerializeField] GameObject confirmacao;

    void OnEnable()
    {
        Atualizar();
    }

    void Atualizar()
    {
        var gm = GameManager.Instance;
        confirmacao.SetActive(false);

        // SetValueWithoutNotify: posiciona o slider sem disparar o evento (e sem salvar à toa).
        sliderPalavrasPorFase.SetValueWithoutNotify(gm.PalavrasPorFase);
        textoPalavrasPorFase.text = gm.PalavrasPorFase.ToString();

        sliderSegundos.SetValueWithoutNotify(gm.SegundosPorPalavra);
        AtualizarTextosTimer();

        foreach (Transform filho in conteudoLista)
        {
            filho.gameObject.SetActive(false); // sai do layout imediatamente
            Destroy(filho.gameObject);
        }

        // Só palavras já jogadas; as mais erradas primeiro.
        var registros = gm.Progresso.palavras
                          .Where(r => r.acertos + r.erros > 0)
                          .OrderByDescending(r => r.erros)
                          .ThenBy(r => r.palavra)
                          .ToList();

        textoVazio.SetActive(registros.Count == 0);

        foreach (var registro in registros)
        {
            var linha = Instantiate(prefabLinha, conteudoLista);
            linha.Configurar(registro.palavra, registro.acertos, registro.erros);
        }
    }

    // ---------- Palavras por fase ----------

    public void AoMudarPalavrasPorFase(float valor)
    {
        var gm = GameManager.Instance;
        gm.DefinirPalavrasPorFase(Mathf.RoundToInt(valor));
        textoPalavrasPorFase.text = gm.PalavrasPorFase.ToString();
    }

    // ---------- Timer ----------

    public void AlternarTimer()
    {
        var gm = GameManager.Instance;
        gm.DefinirTimerAtivo(!gm.TimerAtivo);
        AtualizarTextosTimer();
    }

    public void AoMudarSegundos(float valor)
    {
        var gm = GameManager.Instance;
        gm.DefinirSegundosPorPalavra(Mathf.RoundToInt(valor));
        AtualizarTextosTimer();
    }

    void AtualizarTextosTimer()
    {
        var gm = GameManager.Instance;
        textoTimer.text = gm.TimerAtivo ? "TIMER: LIGADO" : "TIMER: DESLIGADO";
        textoSegundos.text = gm.SegundosPorPalavra + " s";
        sliderSegundos.interactable = gm.TimerAtivo;
    }

    // ---------- Reset ----------

    public void PedirConfirmacao()
    {
        confirmacao.SetActive(true);
    }

    public void CancelarReset()
    {
        confirmacao.SetActive(false);
    }

    public void ConfirmarReset()
    {
        GameManager.Instance.ResetarProgresso();
        Atualizar();
    }
}
