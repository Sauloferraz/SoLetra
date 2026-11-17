using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Botão de uma fase na Trilha: um minijogo em um mundo.
// Mostra as estrelas salvas e abre a cena do minijogo com o mundo escolhido.
public class BotaoFase : MonoBehaviour
{
    [SerializeField] string cena;       // ex.: "MinijogoMonteAPalavra"
    [SerializeField] string prefixoId;  // ex.: "MonteAPalavra" (o id da fase é prefixoId + "_" + mundo)
    [SerializeField] int mundo = 1;
    [SerializeField] bool disponivel = true;

    [SerializeField] Button botao;
    [SerializeField] Image fundo;
    [SerializeField] Image[] estrelas;
    [SerializeField] GameObject avisoEmBreve;

    readonly Color corGanha = new Color32(0xFF, 0xD1, 0x66, 0xFF);
    readonly Color corSemEstrela = new Color(1f, 1f, 1f, 0.35f);
    readonly Color corIndisponivel = new Color32(0xB8, 0xB3, 0xA3, 0xFF);

    void Start()
    {
        if (!disponivel)
        {
            botao.interactable = false;
            fundo.color = corIndisponivel;
            foreach (var estrela in estrelas) estrela.gameObject.SetActive(false);
            avisoEmBreve.SetActive(true);
            return;
        }

        avisoEmBreve.SetActive(false);

        int ganhas = GameManager.Instance.EstrelasDaFase(prefixoId + "_" + mundo);
        for (int i = 0; i < estrelas.Length; i++)
        {
            estrelas[i].gameObject.SetActive(true);
            estrelas[i].color = i < ganhas ? corGanha : corSemEstrela;
        }
    }

    public void Abrir()
    {
        GameManager.Instance.mundoAtual = mundo;
        SceneManager.LoadScene(cena);
    }
}
