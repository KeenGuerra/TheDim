using UnityEngine;

// Gota de estática que cae del cielo durante la pelea con El Susurro.
// Primero parpadea arriba (aviso) y después cae.
// Va en el prefab "GotaEstatica" (Sprite Renderer + Circle Collider 2D con Is Trigger).
public class GotaEstatica : MonoBehaviour
{
    [HideInInspector] public ElSusurro jefe;
    [HideInInspector] public float suelo = 0f;

    public float aviso = 0.5f;
    public float velocidadCaida = 7f;

    private SpriteRenderer sr;
    private float tiempo = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Destroy(gameObject, 6f);
    }

    void Update()
    {
        tiempo += Time.deltaTime;

        if (tiempo < aviso)
        {
            // Parpadea antes de caer para que el jugador lo vea venir
            if (sr != null) sr.enabled = Mathf.FloorToInt(tiempo * 14f) % 2 == 0;
            return;
        }

        if (sr != null) sr.enabled = true;
        transform.position += Vector3.down * velocidadCaida * Time.deltaTime;

        if (transform.position.y < suelo - 0.5f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (tiempo < aviso) return;
        if (otro.GetComponent<MovimientoAlex>() == null) return;

        if (jefe != null) jefe.GolpearAlex(jefe.danoGota);
        Destroy(gameObject);
    }
}
