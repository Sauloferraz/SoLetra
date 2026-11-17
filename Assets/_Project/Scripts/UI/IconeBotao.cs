using UnityEngine;
using UnityEngine.UI;

public class IconeBotao : MonoBehaviour
{
    [SerializeField] string nome;                 // nome do arquivo, sem extensão
    [SerializeField] Image imagem;
    [SerializeField] RectTransform rotulo;        // texto do botão (é empurrado para dar espaço ao ícone)
    [SerializeField] float espacoParaIcone = 100f;

    [Tooltip("Marque se o seu ícone é colorido e não deve ser pintado com a cor do texto.")]
    [SerializeField] bool manterCoresOriginais;

    void Awake()
    {
        var sprite = CarregarSprite(nome);

        imagem.sprite = sprite;
        imagem.enabled = sprite != null;
        if (manterCoresOriginais) imagem.color = Color.white;

        if (rotulo != null)
            rotulo.offsetMin = new Vector2(sprite != null ? espacoParaIcone : 0f, rotulo.offsetMin.y);

        if (sprite == null)
            Debug.LogWarning("[Ícones] Não achei Resources/Icones/" + nome + " (o botão ficará só com texto).", this);
    }

    static Sprite CarregarSprite(string nomeDoArquivo)
    {
        string caminho = "Icones/" + nomeDoArquivo;

        var sprite = Resources.Load<Sprite>(caminho);
        if (sprite != null) return sprite;

        // O PNG existe, mas não foi importado como Sprite: monta o Sprite a partir da textura.
        var textura = Resources.Load<Texture2D>(caminho);
        if (textura == null) return null;

        return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
