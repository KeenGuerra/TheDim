using UnityEngine;

// El haz de luz que sale de la linterna. Vuela recto, calma las sombras que toca y se desvanece.
[RequireComponent(typeof(Rigidbody2D))]
public class HazLuz : MonoBehaviour
{
    public float velocidad = 12f;
    [Tooltip("Distancia máxima que recorre antes de apagarse")]
    public float alcance = 8f;
    public float crecimiento = 0.6f;

    private Vector2 direccion = Vector2.right;
    private Vector3 inicio;
    private Vector3 escalaBase;
    private SpriteRenderer sr;

    // Lo llama LinternaLight al crearlo
    public void Lanzar(Vector2 dir)
    {
        direccion = dir.normalized;
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = direccion.x < 0;
    }

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;   // no le afecta la gravedad
        rb.gravityScale = 0f;
    }

    void Start()
    {
        inicio = transform.position;
        escalaBase = transform.localScale;
    }

    void Update()
    {
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);

        float recorrido = Vector3.Distance(inicio, transform.position);
        float k = Mathf.Clamp01(recorrido / alcance);

        // Crece un poco y se desvanece al final
        transform.localScale = escalaBase * (1f + k * crecimiento);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
            sr.color = c;
        }

        if (recorrido >= alcance) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        SombraComun sombra = otro.GetComponentInParent<SombraComun>();
        if (sombra != null) sombra.Calmar();   // la luz sigue de largo
    }
}