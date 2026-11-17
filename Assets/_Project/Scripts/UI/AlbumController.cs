using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Álbum de figurinhas: uma figurinha por palavra do dicionário, ordenadas por mundo.
// As figurinhas novas ganham o selo "NOVA!" nesta visita; ao abrir, elas passam a ser "vistas".
// Segurar uma figurinha ganha abre o zoom (ZoomFigurina).
public class AlbumController : MonoBehaviour
{
    [SerializeField] RectTransform conteudo;
    [SerializeField] TMP_Text textoContador;
    [SerializeField] AudioSource audioSource;
    [SerializeField] FigurinhaCard prefabCard;
    [SerializeField] ZoomFigurina zoom;

    void Start()
    {
        var gm = GameManager.Instance;
        bool minuscula = gm.Progresso.letraMinuscula;

        var palavras = gm.TodasPalavras
                         .OrderBy(p => p.mundo)
                         .ThenBy(p => p.texto)
                         .ToList();

        int ganhas = 0;
        for (int i = 0; i < palavras.Count; i++)
        {
            var palavra = palavras[i];
            bool tem = gm.TemFigurinha(palavra.texto);
            bool nova = gm.FigurinhaEhNova(palavra.texto);
            if (tem) ganhas++;

            var card = Instantiate(prefabCard, conteudo);
            card.Configurar(palavra, i + 1, tem, nova, minuscula, this);
        }

        textoContador.text = ganhas + " / " + palavras.Count;

        gm.LimparFigurinhasNovas(); // na próxima visita, o selo já não aparece
    }

    public void TocarPalavra(PalavraData palavra)
    {
        if (palavra.audio == null || !GameManager.Instance.Progresso.somLigado || audioSource.isPlaying) return;
        audioSource.PlayOneShot(palavra.audio);
    }

    // Chamado pela figurinha quando o jogador a segura.
    public void MostrarZoom(PalavraData palavra, int posicao, bool minuscula)
    {
        if (zoom != null) zoom.Mostrar(palavra, posicao, minuscula);
        TocarPalavra(palavra);
    }

    public void EsconderZoom()
    {
        if (zoom != null) zoom.Esconder();
    }

    public void VoltarAoMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
