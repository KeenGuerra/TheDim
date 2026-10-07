using UnityEngine;

// Muro de oscuridad que persigue a Alex desde la izquierda.
// Todas las distancias se miden desde su BORDE DERECHO (el frente), no desde el centro.
// Va en un objeto con SpriteRenderer + BoxCollider2D (Is Trigger).
public class OscuridadQuePersigue : MonoBehaviour
{
    [Header("Cuándo empieza y dónde termina")]
    [Tooltip("La oscuridad despierta cuando Alex pasa esta X")]
    public float xActivacion = 0f;
    [Tooltip("El frente de la oscuridad se detiene en esta X")]
    public float xFinal = 50f;

    [Header("Movimiento (relativo a la velocidad de Alex)")]
    [Tooltip("1 = igual que Alex caminando. 1.1 = un poco más rápida: caminando te alcanza de a poco, con RUN escapas.")]
    public float velocidadRelativa = 1.1f;
    [Tooltip("Si se queda más lejos que esto, acelera suavemente para alcanzar")]
    public float distanciaMaxima = 10f;
    [Tooltip("Qué tan rápido recupera distancia cuando se queda atrás (relativo a Alex)")]
    public float velocidadAlcance = 1.4f;
    [Tooltip("Segundos de gracia al despertar antes de empezar a avanzar")]
    public float esperaInicial = 0.5f;

    [Header("Efectos")]
    public float velocidadPulso = 2f;
    public float fuerzaPulso = 0.03f;
    [Tooltip("Sonido grave en bucle (opcional): sube cuando está cerca")]
    public AudioSource rumor;
    public float distanciaRumor = 12f;

    private Transform alex;
    private MovimientoAlex mov;
    private Collider2D col;
    private Vector3 posInicial;
    private Vector3 escalaBase;
    private float offsetFrente;      // distancia del centro al borde derecho
    private bool activa = false;
    private bool alcanzo = false;
    private float tiempoActiva = 0f;

    float Frente => transform.position.x + offsetFrente;

    void Start()
    {
        posInicial = transform.position;
        escalaBase = transform.localScale;
        col = GetComponent<Collider2D>();
        offsetFrente = col != null ? col.bounds.max.x - transform.position.x : 0f;

        mov = FindFirstObjectByType<MovimientoAlex>();
        if (mov != null) alex = mov.transform;
        if (rumor != null) { rumor.loop = true; rumor.volume = 0f; rumor.Play(); }
    }

    void Update()
    {
        // Respira (solo en Y, para que el frente no se mueva solo)
        float p = 1f + Mathf.Sin(Time.time * velocidadPulso) * fuerzaPulso;
        transform.localScale = new Vector3(escalaBase.x, escalaBase.y * p, escalaBase.z);
        if (alex == null) return;

        // Después de alcanzarlo, espera a que Alex vuelva antes de la línea de inicio
        if (alcanzo)
        {
            if (alex.position.x < xActivacion - 1f)
            {
                alcanzo = false;
                transform.position = posInicial;
            }
            ActualizarRumor();
            return;
        }

        if (!activa && alex.position.x > xActivacion)
        {
            activa = true;
            tiempoActiva = 0f;
        }

        if (activa)
        {
            tiempoActiva += Time.deltaTime;
            if (tiempoActiva > esperaInicial)
            {
                float velAlex = mov != null ? mov.velocidad : 5f;
                bool muyAtras = alex.position.x - Frente > distanciaMaxima;
                float vel = velAlex * (muyAtras ? velocidadAlcance : velocidadRelativa);

                float frente = Frente + vel * Time.deltaTime;   // siempre avanza suave, nunca salta
                frente = Mathf.Min(frente, xFinal);
                transform.position = new Vector3(frente - offsetFrente, transform.position.y, transform.position.z);
            }
        }

        ActualizarRumor();
    }

    void ActualizarRumor()
    {
        if (rumor == null) return;
        float d = Mathf.Abs(alex.position.x - Frente);
        float objetivo = activa ? Mathf.Clamp01(1f - d / distanciaRumor) : 0f;
        rumor.volume = Mathf.MoveTowards(rumor.volume, objetivo, Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!activa) return;
        if (otro.GetComponent<MovimientoAlex>() == null) return;

        // Alcanzó a Alex: se corta la conexión y la oscuridad vuelve a su lugar
        activa = false;
        alcanzo = true;
        if (CorruptionManager.instancia != null)
            CorruptionManager.instancia.SubirCorrupcion(100f);
        transform.position = posInicial;
    }

    void OnDrawGizmosSelected()
    {
        float y = transform.position.y;
        Gizmos.color = Color.yellow; Gizmos.DrawLine(new Vector3(xActivacion, y - 8, 0), new Vector3(xActivacion, y + 8, 0));
        Gizmos.color = Color.magenta; Gizmos.DrawLine(new Vector3(xFinal, y - 8, 0), new Vector3(xFinal, y + 8, 0));
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) { Gizmos.color = Color.red; Gizmos.DrawLine(new Vector3(c.bounds.max.x, y - 8, 0), new Vector3(c.bounds.max.x, y + 8, 0)); }
    }
}