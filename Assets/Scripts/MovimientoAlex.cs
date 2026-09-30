using UnityEngine;

public class MovimientoAlex : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 8f;

    [Header("Detección de suelo")]
    public Transform puntoSuelo;
    public float radioDeteccion = 0.25f;
    public LayerMask capaSuelo;

    [Header("Animación")]
    [Tooltip("Tiempo que Alex puede perder el suelo sin que cambie a la animación de salto")]
    public float margenSinSuelo = 0.15f;

    // Nombres EXACTOS de las cajas del Animator
    private const string ANIM_IDLE = "Alex_Idle";
    private const string ANIM_RUN = "Alex_Run";
    private const string ANIM_JUMP = "Alex_Jump";

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private bool enElSuelo;
    private float tiempoSinSuelo;
    private string animacionActual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        CambiarAnimacion(ANIM_IDLE);
    }

    void Update()
    {
        // Detectar si está tocando el suelo
        enElSuelo = Physics2D.OverlapCircle(puntoSuelo.position, radioDeteccion, capaSuelo);

        // Moverse (flechas izquierda/derecha o A/D)
        float movimientoHorizontal = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(movimientoHorizontal * velocidad, rb.linearVelocity.y);

        // Mirar hacia donde camina
        if (movimientoHorizontal > 0) sr.flipX = false;
        else if (movimientoHorizontal < 0) sr.flipX = true;

        // Saltar con Espacio, W o flecha arriba
        if (PresionoSaltar() && enElSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            tiempoSinSuelo = 1f; // pasa a la animación de salto al instante
        }

        // Contar cuánto tiempo lleva sin tocar el suelo
        if (enElSuelo && rb.linearVelocity.y <= 0.1f)
            tiempoSinSuelo = 0f;
        else
            tiempoSinSuelo += Time.deltaTime;

        bool pisando = tiempoSinSuelo < margenSinSuelo;

        // Elegir la animación
        if (!pisando)
            CambiarAnimacion(ANIM_JUMP);
        else if (movimientoHorizontal != 0)
            CambiarAnimacion(ANIM_RUN);
        else
            CambiarAnimacion(ANIM_IDLE);
    }

    // Todas las teclas que sirven para saltar
    bool PresionoSaltar()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.UpArrow);
    }

    // Solo cambia de animación si es distinta a la actual (así no se reinicia cada frame)
    void CambiarAnimacion(string nueva)
    {
        if (animacionActual == nueva) return;
        anim.Play(nueva);
        animacionActual = nueva;
    }

    // Dibuja el círculo amarillo de detección en la vista Scene
    void OnDrawGizmos()
    {
        if (puntoSuelo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(puntoSuelo.position, radioDeteccion);
        }
    }
}