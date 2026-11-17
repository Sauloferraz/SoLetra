using System;
using System.Collections.Generic;

[Serializable]
public class RegistroPalavra
{
    public string palavra;
    public int acertos;
    public int erros;
}

[Serializable]
public class RegistroFase
{
    public string id;      // ex.: "MonteAPalavra_1" (minijogo + mundo)
    public int estrelas;   // melhor resultado (0 a 3)
}

[Serializable]
public class ProgressoSalvo
{
    public List<RegistroPalavra> palavras = new List<RegistroPalavra>();
    public List<RegistroFase> fases = new List<RegistroFase>();
    public List<string> figurinhas = new List<string>();
    public List<string> figurinhasNovas = new List<string>(); // ganhas e ainda não vistas no álbum

    // Configurações do professor
    public int palavrasPorFase = 5;
    public bool timerAtivo = false;
    public int segundosPorPalavra = 30;

    // Ajustes
    public bool somLigado = true;
    public bool letraMinuscula = false;
}
