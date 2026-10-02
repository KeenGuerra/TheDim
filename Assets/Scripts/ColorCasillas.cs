using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Pinta cada casilla del letrero según las letras que ya recogió el jugador.
// Va en el mismo objeto que el Tablero (Tablero_LIGHT).
public class ColorCasillas : MonoBehaviour
{
    [Tooltip("La palabra de este letrero")]
    public string palabra = "LIGHT";

    [Header("Colores")]
    public Color colorFaltante = new Color(0.35f, 0.33f, 0.38f, 0.5f);   // gris tenue
    public Color colorTenida = new Color(0.95f, 0.65f, 0.25f, 1f);     // ámbar
    public Color colorEncendida = new Color(1f, 0.9f, 0.63f, 1f);        // amarillo claro

    [Header("Efecto al recoger")]
    public float escalaPop = 1.4f;
    public float velocidadPop = 6f;

    private List<TextMeshPro> casillas = new List<TextMeshPro>();
    private bool[] vista;      // ya vimos esa letra en el nivel
    private bool[] tenida;     // ya la recogió
    private float[] pop;
    private Vector3[] escalaBase;
    private float siguienteRevision = 0f;

    void Start()
    {
        // Busca los textos de las casillas (hijos llamados "Casilla...")
        foreach (TextMeshPro t in GetComponentsInChildren<TextMeshPro>(true))
            if (t.name.StartsWith("Casilla")) casillas.Add(t);

        // Las ordena de izquierda a derecha
        casillas.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        int n = casillas.Count;
        vista = new bool[n];
        tenida = new bool[n];
        pop = new float[n];
        escalaBase = new Vector3[n];
        for (int i = 0; i < n; i++) escalaBase[i] = casillas[i].transform.localScale;

        Revisar();
    }

    void Update()
    {
        if (Time.time >= siguienteRevision)
        {
            siguienteRevision = Time.time + 0.15f;
            Revisar();
        }
    }

    void LateUpdate()
    {
        bool aprendida = WordManager.instancia != null && WordManager.instancia.PalabraAprendida(palabra);

        for (int i = 0; i < casillas.Count; i++)
        {
            if (aprendida) casillas[i].color = colorEncendida;
            else casillas[i].color = tenida[i] ? colorTenida : colorFaltante;

            // Pequeño "salto" cuando la letra se acaba de recoger
            pop[i] = Mathf.MoveTowards(pop[i], 0f, Time.deltaTime * velocidadPop);
            casillas[i].transform.localScale = escalaBase[i] * (1f + pop[i] * (escalaPop - 1f));
        }
    }

    // Una letra cuenta como recogida si estaba en el nivel y ya no está
    void Revisar()
    {
        Letra[] letras = FindObjectsByType<Letra>(FindObjectsSortMode.None);

        for (int i = 0; i < casillas.Count; i++)
        {
            if (tenida[i]) continue;

            char c = LetraDeCasilla(i);
            bool existe = false;
            foreach (Letra l in letras)
                if (char.ToUpper(l.letra) == c && l.gameObject.activeInHierarchy) { existe = true; break; }

            if (existe) vista[i] = true;
            else if (vista[i])
            {
                tenida[i] = true;
                pop[i] = 1f;
            }
        }
    }

    char LetraDeCasilla(int i)
    {
        if (i < palabra.Length) return char.ToUpper(palabra[i]);
        string t = casillas[i].text;
        return string.IsNullOrEmpty(t) ? ' ' : char.ToUpper(t[0]);
    }
}