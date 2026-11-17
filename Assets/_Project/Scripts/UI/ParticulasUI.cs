using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ParticulasUI : MonoBehaviour
{
    public static ParticulasUI Instance { get; private set; }

    [SerializeField] Sprite spriteConfete;
    [SerializeField] Sprite spriteEstrela;
    [SerializeField] int tamanhoDoPool = 120;

    static readonly Color[] CoresConfete =
    {
        new Color32(0x1B, 0x99, 0x8B, 0xFF), // turquesa
        new Color32(0xFF, 0xD1, 0x66, 0xFF), // amarelo
        new Color32(0xFF, 0x6B, 0x6B, 0xFF), // coral
        new Color32(0x9B, 0x5D, 0xE5, 0xFF), // roxo
        new Color32(0x3D, 0x8B, 0xFD, 0xFF), // azul
        new Color32(0x2F, 0xBF, 0x71, 0xFF), // verde
    };

    static readonly Color[] CoresFaisca =
    {
        new Color32(0xFF, 0xD1, 0x66, 0xFF),
        new Color32(0xFF, 0xF1, 0xB8, 0xFF),
        Color.white,
    };

    class Peca
    {
        public RectTransform rect;
        public Image imagem;
        public bool ativa;
        public Vector2 pos, vel;
        public float gravidade, arrasto, angulo, giro, vida, duracao;
        public Color cor;
        public bool estrela;
    }

    RectTransform raiz;
    Peca[] pool;
    int cursor;
    int ativas;

    void Awake()
    {
        Instance = this;
        raiz = (RectTransform)transform;

        pool = new Peca[Mathf.Max(10, tamanhoDoPool)];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("Peca", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(raiz, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

            var imagem = go.GetComponent<Image>();
            imagem.raycastTarget = false;
            imagem.enabled = false;

            pool[i] = new Peca { rect = rect, imagem = imagem };
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- API estática ----------

    public static void Faiscas(RectTransform alvo, int quantidade)
    {
        if (Instance != null && alvo != null) Instance.CriarFaiscas(Instance.PosicaoLocalDe(alvo), quantidade);
    }

    public static void Confete(RectTransform alvo, int quantidade)
    {
        if (Instance != null && alvo != null) Instance.CriarConfete(Instance.PosicaoLocalDe(alvo), quantidade);
    }

    public static void ChuvaDeConfete(int quantidade, float segundos)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.Chuva(quantidade, segundos));
    }

    // ---------- Efeitos ----------

    void CriarFaiscas(Vector2 origem, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var peca = Pegar();
            if (peca == null) return;

            float angulo = Random.Range(0f, Mathf.PI * 2f);
            Vector2 velocidade = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * Random.Range(250f, 560f);
            float lado = Random.Range(26f, 48f);

            Ligar(peca, spriteEstrela, new Vector2(lado, lado), CoresFaisca[Random.Range(0, CoresFaisca.Length)],
                  origem, velocidade, 350f, 2.5f, Random.Range(-300f, 300f), Random.Range(0.7f, 1.1f), true);
        }
    }

    void CriarConfete(Vector2 origem, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var peca = Pegar();
            if (peca == null) return;

            float angulo = Random.Range(0f, Mathf.PI * 2f);
            Vector2 velocidade = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * Random.Range(450f, 950f);
            velocidade.y += 250f; // viés para cima

            Ligar(peca, spriteConfete, TamanhoDeConfete(), CoresConfete[Random.Range(0, CoresConfete.Length)],
                  origem, velocidade, Random.Range(1400f, 1900f), 1.2f, Random.Range(-540f, 540f),
                  Random.Range(1.4f, 2.2f), false);
        }
    }

    IEnumerator Chuva(int quantidade, float segundos)
    {
        float intervalo = segundos / Mathf.Max(1, quantidade);

        for (int i = 0; i < quantidade; i++)
        {
            var peca = Pegar();
            if (peca != null)
            {
                Vector2 origem = new Vector2(Random.Range(-raiz.rect.width / 2f, raiz.rect.width / 2f),
                                             raiz.rect.height / 2f + 40f);
                Vector2 velocidade = new Vector2(Random.Range(-120f, 120f), Random.Range(-450f, -250f));

                Ligar(peca, spriteConfete, TamanhoDeConfete(), CoresConfete[Random.Range(0, CoresConfete.Length)],
                      origem, velocidade, 120f, 0.3f, Random.Range(-360f, 360f), Random.Range(3f, 4.2f), false);
            }
            yield return new WaitForSeconds(intervalo);
        }
    }

    static Vector2 TamanhoDeConfete()
    {
        return new Vector2(Random.Range(16f, 28f), Random.Range(9f, 15f));
    }

    // ---------- Pool e simulação ----------

    Peca Pegar()
    {
        for (int k = 0; k < pool.Length; k++)
        {
            int i = (cursor + k) % pool.Length;
            if (!pool[i].ativa)
            {
                cursor = i + 1;
                return pool[i];
            }
        }
        return null; // pool esgotado: ignora em vez de criar objetos no meio do jogo
    }

    void Ligar(Peca p, Sprite sprite, Vector2 tamanho, Color cor, Vector2 pos, Vector2 vel,
               float gravidade, float arrasto, float giro, float duracao, bool estrela)
    {
        p.ativa = true;
        p.vida = 0f;
        p.duracao = duracao;
        p.pos = pos;
        p.vel = vel;
        p.gravidade = gravidade;
        p.arrasto = arrasto;
        p.angulo = Random.Range(0f, 360f);
        p.giro = giro;
        p.cor = cor;
        p.estrela = estrela;

        p.imagem.sprite = sprite;
        p.imagem.color = cor;
        p.imagem.enabled = true;
        p.rect.sizeDelta = tamanho;
        p.rect.anchoredPosition = pos;
        p.rect.localScale = Vector3.zero;

        ativas++;
    }

    void Update()
    {
        if (ativas == 0) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < pool.Length; i++)
        {
            var p = pool[i];
            if (!p.ativa) continue;

            p.vida += dt;
            if (p.vida >= p.duracao)
            {
                p.ativa = false;
                p.imagem.enabled = false;
                ativas--;
                continue;
            }

            p.vel.y -= p.gravidade * dt;
            p.vel *= Mathf.Max(0f, 1f - p.arrasto * dt);
            p.pos += p.vel * dt;
            p.angulo += p.giro * dt;

            float t = p.vida / p.duracao;
            float alfa = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            float escala = Mathf.Min(1f, p.vida / 0.12f);   // surge crescendo
            if (p.estrela) escala *= 1f - t * 0.6f;          // estrelinhas encolhem

            p.rect.anchoredPosition = p.pos;
            p.rect.localRotation = Quaternion.Euler(0f, 0f, p.angulo);
            p.rect.localScale = new Vector3(escala, escala, 1f);

            var cor = p.cor;
            cor.a = alfa;
            p.imagem.color = cor;
        }
    }

    // Posição do centro de um RectTransform qualquer, no espaço local da camada de partículas.
    Vector2 PosicaoLocalDe(RectTransform alvo)
    {
        Vector2 naTela = RectTransformUtility.WorldToScreenPoint(null, alvo.TransformPoint(alvo.rect.center));
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, naTela, null, out local);
        return local;
    }
}
