using System.Collections;
using UnityEngine;
using TMPro;

public class Letra : MonoBehaviour
{
    [Tooltip("La letra que representa este objeto. Ej: L, I, G, H, T")]
    public char letra;

    [Header("Visual")]
    [Tooltip("El texto hijo que muestra la letra. Si está vacío, se busca solo.")]
    public TextMeshPro texto;
    public float alturaFlote = 0.15f;
    public float velocidadFlote = 2f;
    public float fuerzaPulso = 0.06f;
    public float velocidadPulso = 3f;

    [Header("Al recogerla")]
    public float duracionRecoger = 0.35f;

    private SpriteRenderer sr;
    private Vector3 posicionInicial;
    private Vector3 escalaBase;
    private float desfase;
    private bool recogida = false;

    // Se ejecuta en el editor al cambiar algo en el Inspector:
    // así ves la letra correcta en el orbe sin darle Play
    void OnValidate()
    {
        if (texto == null) texto = GetComponentInChildren<TextMeshPro>();
        if (texto != null) texto.text = char.ToUpper(letra).ToString();
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (texto == null) texto = GetComponentInChildren<TextMeshPro>();

        posicionInicial = transform.position;
        escalaBase = transform.localScale;
        desfase = Random.Range(0f, 10f); // para que no floten todas al mismo ritmo

        if (texto != null) texto.text = char.ToUpper(letra).ToString();
    }

    void Update()
    {
        if (recogida) return;

        float t = Time.time + desfase;

        // Flotar suavemente arriba y abajo
        transform.position = posicionInicial + Vector3.up * (Mathf.Sin(t * velocidadFlote) * alturaFlote);

        // Latir como una luz
        float pulso = 1f + Mathf.Sin(t * velocidadPulso) * fuerzaPulso;
        transform.localScale = escalaBase * pulso;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recogida || !other.CompareTag("Player")) return;

        recogida = true;
        WordManager.instancia.RecolectarLetra(char.ToUpper(letra));
        StartCoroutine(Recoger());
    }

    IEnumerator Recoger()
    {
        // Ya no se puede volver a tocar
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 inicio = transform.position;
        Color colorOrbe = sr != null ? sr.color : Color.white;
        Color colorTexto = texto != null ? texto.color : Color.white;
        float t = 0f;

        // Sube, crece y se desvanece
        while (t < duracionRecoger)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionRecoger);

            transform.position = inicio + Vector3.up * (0.6f * k);
            transform.localScale = escalaBase * (1f + 0.6f * k);

            if (sr != null)
                sr.color = new Color(colorOrbe.r, colorOrbe.g, colorOrbe.b, colorOrbe.a * (1f - k));
            if (texto != null)
                texto.color = new Color(colorTexto.r, colorTexto.g, colorTexto.b, colorTexto.a * (1f - k));

            yield return null;
        }

        Destroy(gameObject);
    }
}