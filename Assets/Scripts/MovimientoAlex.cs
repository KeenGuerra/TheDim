using UnityEngine;

public class MovimientoAlex : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;

    [Header("Salto")]
    public float fuerzaSalto = 8f;
    [Tooltip("Fuerza del segundo salto (en el aire)")]
    public float fuerzaSegundoSalto = 7f;
    [Tooltip("Cuántos saltos extra tiene en el aire (1 = doble salto)")]
    public int saltosExtra = 1;

    [Header("Detección de suelo")]
    public Transform puntoSuelo;
    public float radioDeteccion = 0.25f;
    public LayerMask capaSuelo;
    [Tooltip("Tiempo de tolerancia al dejar el suelo (evita parpadeos y permite saltar justo al borde)")]
    public float margenSinSuelo = 0.15f;

    [Header("Modificadores (los usa HabilidadRun)")]
    [HideInInspector] public float multiplicadorVelocidad = 1f;
    [HideInInspector] public float multiplicadorSalto = 1f;

    [Header("Efecto opcional del segundo salto")]
    public ParticleSystem efectoSegundoSalto;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private float movimiento;
    private bool enSuelo;
    private float tiempoSinSuelo;
    private int saltosRestantes;
    private string animacionActual = "";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        saltosRestantes = saltosExtra;
    }

    void Update()
    {
        // --- Movimiento horizontal ---
        movimiento = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(movimiento * velocidad * multiplicadorVelocidad, rb.linearVelocity.y);

        if (movimiento > 0) sr.flipX = false;
        else if (movimiento < 0) sr.flipX = true;

        // --- Suelo ---
        bool tocaSuelo = puntoSuelo != null &&
                         Physics2D.OverlapCircle(puntoSuelo.position, radioDeteccion, capaSuelo);

        if (tocaSuelo && rb.linearVelocity.y <= 0.05f)
        {
            enSuelo = true;
            tiempoSinSuelo = 0f;
            saltosRestantes = saltosExtra;
        }
        else
        {
            tiempoSinSuelo += Time.deltaTime;
            if (tiempoSinSuelo > margenSinSuelo) enSuelo = false;
        }

        // --- Salto ---
        if (PresionoSaltar())
        {
            if (enSuelo)
            {
                Saltar(fuerzaSalto);
                AudioJuego.Sonar("salto");                       // SONIDO
                enSuelo = false;
                tiempoSinSuelo = margenSinSuelo + 1f;
            }
            else if (saltosRestantes > 0)
            {
                saltosRestantes--;
                Saltar(fuerzaSegundoSalto);
                AudioJuego.Sonar("doble_salto");                 // SONIDO
                anim.Play("Alex_Jump", 0, 0f);
                animacionActual = "Alex_Jump";
                if (efectoSegundoSalto != null) efectoSegundoSalto.Play();
            }
        }

        CambiarAnimacion();
    }

    // Hacia dónde mira Alex (lo usan otros scripts)
    public bool MirandoIzquierda() { return sr != null && sr.flipX; }

    void Saltar(float fuerza)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * fuerza * multiplicadorSalto, ForceMode2D.Impulse);
    }

    bool PresionoSaltar()
    {
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.UpArrow);
    }

    void CambiarAnimacion()
    {
        string nueva;
        if (!enSuelo) nueva = "Alex_Jump";
        else if (movimiento != 0) nueva = "Alex_Run";
        else nueva = "Alex_Idle";

        if (nueva != animacionActual)
        {
            anim.Play(nueva);
            animacionActual = nueva;
        }
    }

    void OnDrawGizmos()
    {
        if (puntoSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(puntoSuelo.position, radioDeteccion);
    }
}
