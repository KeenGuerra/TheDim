using System.Collections;
using UnityEngine;

// Tabla de puente: al pisarla tiembla, se cae y después vuelve a aparecer.
// Va en cada tabla (objeto con SpriteRenderer + BoxCollider2D, Layer Suelo).
public class PuenteQueSeCae : MonoBehaviour
{
    [Tooltip("Segundos que tiembla antes de caer")]
    public float tiempoAntesDeCaer = 0.45f;
    [Tooltip("Segundos hasta que vuelve a aparecer")]
    public float tiempoReaparecer = 3f;
    public float fuerzaTemblor = 0.06f;
    public float distanciaCaida = 4f;
    public float duracionCaida = 0.6f;

    private Vector3 posInicial;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool activada = false;

    void Start()
    {
        posInicial = transform.position;
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnCollisionEnter2D(Collision2D otro)
    {
        if (activada) return;
        if (otro.gameObject.GetComponent<MovimientoAlex>() == null) return;

        // Solo si Alex la pisa desde arriba
        if (otro.collider.bounds.min.y < col.bounds.max.y - 0.15f) return;

        StartCoroutine(Caer());
    }

    IEnumerator Caer()
    {
        activada = true;
        AudioJuego.Sonar("tabla");

        // Tiembla
        float t = 0f;
        while (t < tiempoAntesDeCaer)
        {
            t += Time.deltaTime;
            transform.position = posInicial + (Vector3)(Random.insideUnitCircle * fuerzaTemblor);
            yield return null;
        }

        // Cae y se desvanece
        col.enabled = false;
        Color c = sr != null ? sr.color : Color.white;
        t = 0f;
        while (t < duracionCaida)
        {
            t += Time.deltaTime;
            float k = t / duracionCaida;
            transform.position = posInicial + Vector3.down * distanciaCaida * k * k;
            transform.rotation = Quaternion.Euler(0, 0, 15f * k);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, 1f - k);
            yield return null;
        }
        if (sr != null) sr.enabled = false;

        // Espera y reaparece
        yield return new WaitForSeconds(tiempoReaparecer);
        transform.position = posInicial;
        transform.rotation = Quaternion.identity;
        if (sr != null) { sr.enabled = true; sr.color = new Color(c.r, c.g, c.b, 0f); }
        col.enabled = true;

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, t / 0.4f);
            yield return null;
        }
        if (sr != null) sr.color = c;
        activada = false;
    }
}
