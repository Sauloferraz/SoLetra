using UnityEngine;

[CreateAssetMenu(fileName = "NovaPalavra", menuName = "SóLetra/Palavra")]
public class PalavraData : ScriptableObject
{
    [Range(1, 3)] public int mundo = 1;

    [Tooltip("Sempre em MAIÚSCULAS, sem acento. Ex.: BOLA")]
    public string texto;

    [Tooltip("Ex.: { \"BO\", \"LA\" }. Juntas, precisam formar o texto.")]
    public string[] silabas;

    public Sprite imagem;
    public AudioClip audio;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(texto) || silabas == null || silabas.Length == 0) return;

        string junto = string.Concat(silabas);
        if (junto != texto)
            Debug.LogWarning($"[{name}] As sílabas ({junto}) não formam o texto ({texto}).", this);
    }
}
