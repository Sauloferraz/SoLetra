using TMPro;
using UnityEngine;

// Uma linha da lista de desempenho do painel do professor.
public class LinhaDesempenho : MonoBehaviour
{
    [SerializeField] TMP_Text palavra;
    [SerializeField] TMP_Text acertos;
    [SerializeField] TMP_Text erros;

    public void Configurar(string nomePalavra, int totalAcertos, int totalErros)
    {
        palavra.text = nomePalavra;
        acertos.text = totalAcertos.ToString();
        erros.text = totalErros.ToString();
    }
}
