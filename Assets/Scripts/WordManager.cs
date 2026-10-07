using System.Collections.Generic;
using UnityEngine;

public class WordManager : MonoBehaviour
{
    public static WordManager instancia;

    [Header("Estado del jugador")]
    public List<char> letrasRecolectadas = new List<char>();
    [Tooltip("Palabras que el jugador ya sabe al empezar este nivel (ej: en el Nivel 2, LIGHT)")]
    public List<string> palabrasAprendidas = new List<string>();

    [Header("Guardado")]
    [Tooltip("Si está marcado, también carga las palabras guardadas al terminar el nivel anterior. Déjalo DESMARCADO en el Nivel 1.")]
    public bool cargarPalabrasGuardadas = false;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Todas en mayúsculas, sin espacios
        for (int i = 0; i < palabrasAprendidas.Count; i++)
            palabrasAprendidas[i] = palabrasAprendidas[i].Trim().ToUpper();

        if (cargarPalabrasGuardadas) CargarGuardadas();
    }

    void CargarGuardadas()
    {
        string guardado = PlayerPrefs.GetString("PalabrasAprendidas", "");
        if (string.IsNullOrEmpty(guardado)) return;

        foreach (string p in guardado.Split(new char[] { ',', ';', '|', ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            string palabra = p.Trim().ToUpper();
            if (!palabrasAprendidas.Contains(palabra)) palabrasAprendidas.Add(palabra);
        }
        Debug.Log("Palabras cargadas: " + string.Join(", ", palabrasAprendidas));
    }

    public void RecolectarLetra(char letra)
    {
        letrasRecolectadas.Add(char.ToUpper(letra));
        Debug.Log("Letra recolectada: " + letra + " | Total: " + string.Join("", letrasRecolectadas));
    }

    public bool TieneLetrasPara(string palabra)
    {
        List<char> copiaLetras = new List<char>(letrasRecolectadas);
        foreach (char c in palabra.ToUpper())
        {
            if (!copiaLetras.Contains(c)) return false;
            copiaLetras.Remove(c);
        }
        return true;
    }

    public void FormarPalabra(string palabra)
    {
        foreach (char c in palabra.ToUpper())
            letrasRecolectadas.Remove(c);

        if (!palabrasAprendidas.Contains(palabra.ToUpper()))
            palabrasAprendidas.Add(palabra.ToUpper());

        Debug.Log("¡Palabra formada: " + palabra + "!");
    }

    public bool PalabraAprendida(string palabra)
    {
        return palabrasAprendidas.Contains(palabra.ToUpper());
    }
}
