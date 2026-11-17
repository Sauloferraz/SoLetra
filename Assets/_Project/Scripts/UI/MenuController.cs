using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject painelAjustes;
    [SerializeField] GameObject painelProfessor;
    [SerializeField] TMP_Text textoSom;
    [SerializeField] TMP_Text textoLetras;

    void Start()
    {
        painelAjustes.SetActive(false);
        painelProfessor.SetActive(false);
        AtualizarTextosAjustes();
        
        SomManager.TocarMusica(SomId.Menu);
    }

    public void Jogar()
    {
        SomManager.PararMusica();
        SceneManager.LoadScene("Trilha");
    }

    public void AbrirAlbum()
    {
        SceneManager.LoadScene("Album");
    }

    public void AbrirAjustes()
    {
        AtualizarTextosAjustes();
        painelAjustes.SetActive(true);
    }

    public void FecharAjustes()
    {
        painelAjustes.SetActive(false);
    }

    // Chamado pelo BotaoSegurar depois de 3 segundos.
    public void AbrirProfessor()
    {
        painelProfessor.SetActive(true);
    }

    public void FecharProfessor()
    {
        painelProfessor.SetActive(false);
    }

    public void AlternarSom()
    {
        var progresso = GameManager.Instance.Progresso;
        progresso.somLigado = !progresso.somLigado;
        GameManager.Instance.Salvar();
        AtualizarTextosAjustes();
    }

    public void AlternarLetras()
    {
        var progresso = GameManager.Instance.Progresso;
        progresso.letraMinuscula = !progresso.letraMinuscula;
        GameManager.Instance.Salvar();
        AtualizarTextosAjustes();
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void AtualizarTextosAjustes()
    {
        var progresso = GameManager.Instance.Progresso;
        textoSom.text = progresso.somLigado ? "EFEITOS SONOROS: SIM" : "EFEITOS SONOROS: NÃO";
        textoLetras.text = progresso.letraMinuscula ? "LETRAS: abc" : "LETRAS: ABC";
    }
}
