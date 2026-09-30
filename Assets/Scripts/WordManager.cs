using System.Collections.Generic;
using UnityEngine;

public class WordManager : MonoBehaviour
{
    public static WordManager instancia;

    [Header("Estado del jugador")]
    public List<char> letrasRecolectadas = new List<char>();
    public List<string> palabrasAprendidas = new List<string>();

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecolectarLetra(char letra)
    {
        letrasRecolectadas.Add(letra);
        Debug.Log("Letra recolectada: " + letra + " | Total: " + string.Join("", letrasRecolectadas));
    }

    public bool TieneLetrasPara(string palabra)
    {
        List<char> copiaLetras = new List<char>(letrasRecolectadas);
        foreach (char c in palabra.ToUpper())
        {
            if (!copiaLetras.Contains(c))
            {
                return false;
            }
            copiaLetras.Remove(c);
        }
        return true;
    }

    public void FormarPalabra(string palabra)
    {
        foreach (char c in palabra.ToUpper())
        {
            letrasRecolectadas.Remove(c);
        }
        if (!palabrasAprendidas.Contains(palabra.ToUpper()))
        {
            palabrasAprendidas.Add(palabra.ToUpper());
        }
        Debug.Log("¡Palabra formada: " + palabra + "!");
    }

    public bool PalabraAprendida(string palabra)
    {
        return palabrasAprendidas.Contains(palabra.ToUpper());
    }
}