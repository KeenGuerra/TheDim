using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

public class Tablero : MonoBehaviour
{
    [Tooltip("La palabra que este tablero forma, en mayúsculas. Ej: LIGHT")]
    public string palabraObjetivo = "LIGHT";

    [Header("Sprites")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Referencias (hijos)")]
    [Tooltip("Una casilla por letra, en orden de izquierda a derecha")]
    public TextMeshPro[] casillas;
    public Light2D luz;
    public ParticleSystem chispas;
    public GameObject avisoTecla;   // opcional

    [Header("Colores de las letras")]
    public Color colorFaltante = new Color(0.45f, 0.4f, 0.5f, 0.5f);
    public Color colorTenida = new Color(0.95f, 0.65f, 0.25f, 1f);
    public Color colorEncendida = new Color(1f, 0.85f, 0.45f, 1f);

    [Header("Al formar la palabra")]
    public float intensidadLuz = 1.5f;
    public float bajaCorrupcion = 10f;

    private SpriteRenderer sr;
    private bool jugadorCerca = false;
    private bool formada = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (spriteApagado != null) sr.sprite = spriteApagado;
        if (luz != null) luz.intensity = 0f;
        if (avisoTecla != null) avisoTecla.SetActive(false);
    }

    void Update()
    {
        ActualizarCasillas();

        bool tieneTodas = WordManager.instancia.TieneLetrasPara(palabraObjetivo);

        if (avisoTecla != null)
            avisoTecla.SetActive(jugadorCerca && !formada && tieneTodas);

        if (jugadorCerca && !formada && Input.GetKeyDown(KeyCode.E))
        {
            if (tieneTodas) StartCoroutine(Formar());
            else StartCoroutine(Sacudir());
        }
    }

    // Cada casilla muestra su letra: gris si falta, ámbar si ya la tienes
    void ActualizarCasillas()
    {
        if (casillas == null) return;

        List<char> disponibles = new List<char>(WordManager.instancia.letrasRecolectadas);
        string palabra = palabraObjetivo.ToUpper();

        for (int i = 0; i < casillas.Length && i < palabra.Length; i++)
        {
            if (casillas[i] == null) continue;

            char c = palabra[i];
            bool laTiene = formada || disponibles.Remove(c);

            casillas[i].text = c.ToString();
            casillas[i].color = formada ? colorEncendida : (laTiene ? colorTenida : colorFaltante);
        }
    }

    IEnumerator Formar()
    {
        formada = true;
        WordManager.instancia.FormarPalabra(palabraObjetivo);
        CorruptionManager.instancia.BajarCorrupcion(bajaCorrupcion);

        // Parpadeo como un letrero viejo que vuelve a la vida
        float[] pausas = { 0.08f, 0.12f, 0.06f, 0.2f, 0.1f };
        bool prendido = false;
        foreach (float p in pausas)
        {
            prendido = !prendido;
            sr.sprite = prendido ? spriteEncendido : spriteApagado;
            if (luz != null) luz.intensity = prendido ? intensidadLuz : 0f;
            yield return new WaitForSeconds(p);
        }

        // Queda encendido
        sr.sprite = spriteEncendido;
        if (chispas != null) chispas.Play();

        // Destello y luego una luz cálida permanente
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (luz != null) luz.intensity = Mathf.Lerp(intensidadLuz * 1.8f, intensidadLuz, t / 0.4f);
            yield return null;
        }

        Debug.Log("¡Palabra formada: " + palabraObjetivo + "!");
    }

    // Las casillas tiemblan si intentas formar la palabra sin todas las letras
    IEnumerator Sacudir()
    {
        if (casillas == null) yield break;

        Vector3[] originales = new Vector3[casillas.Length];
        for (int i = 0; i < casillas.Length; i++)
            if (casillas[i] != null) originales[i] = casillas[i].transform.localPosition;

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float dx = Mathf.Sin(t * 60f) * 0.08f;
            for (int i = 0; i < casillas.Length; i++)
                if (casillas[i] != null)
                    casillas[i].transform.localPosition = originales[i] + Vector3.right * dx;
            yield return null;
        }

        for (int i = 0; i < casillas.Length; i++)
            if (casillas[i] != null) casillas[i].transform.localPosition = originales[i];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) jugadorCerca = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) jugadorCerca = false;
    }
}