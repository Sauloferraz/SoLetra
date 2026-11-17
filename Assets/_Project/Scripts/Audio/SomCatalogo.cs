using System;
using System.Collections.Generic;
using UnityEngine;

public enum SomCategoria { Efeito, Alerta, Interface }

// Um som do jogo. O "nome" vira SomId.Nome (gerado pelo Painel de Sons).
[Serializable]
public class SomEntrada
{
    public string nome = "NovoSom";
    public SomCategoria categoria = SomCategoria.Efeito;

    [Tooltip("Se houver mais de um clipe, um deles é sorteado a cada vez.")]
    public AudioClip[] clipes = new AudioClip[0];

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Variação aleatória do tom (0 = sempre igual).")]
    [Range(0f, 0.5f)] public float variacaoTom = 0f;

    [Tooltip("Tempo mínimo entre duas execuções do mesmo som.")]
    [Min(0f)] public float intervaloMinimo = 0f;
}

// Lista de todos os sons. Fica em Assets/_Project/Resources/CatalogoSons.asset
// e é editada pelo Painel de Sons (menu SóLetra > Painel de Sons).
public class SomCatalogo : ScriptableObject
{
    public List<SomEntrada> sons = new List<SomEntrada>();
}
