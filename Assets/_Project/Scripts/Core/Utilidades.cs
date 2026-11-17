using System.Collections.Generic;
using UnityEngine;

public static class Utilidades
{
    // Fisher-Yates: embaralha a lista no lugar.
    public static void Embaralhar<T>(IList<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = lista[i];
            lista[i] = lista[j];
            lista[j] = temp;
        }
    }
}
