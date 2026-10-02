using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PuntoSeguro : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Luz")]
    public Light2D luz;
    public float intensidadLuz = 1.2f;

    [Header("Encender")]
    [Tooltip("Segundos que Alex debe quedarse quieto bajo el farol para encenderlo")]
    public float tiempoParaEncender = 1f;
    [Tooltip("Velocidad máxima para considerar que Alex está quieto")]
    public float velocidadQuieto = 0.2f;

    [Header("Reaparición")]
    public Vector2 offsetReaparicion = new Vector2(1.5f, 0.5f);

    public bool encendido { get; private set; }
    public Vector3 PosicionReaparicion => transform.position + (Vector3)offsetReaparicion;

    private SpriteRenderer sr;
    private Rigidbody2D rbAlex;
    private bool alexDentro = false;
    private bool zonaSeguraActiva = false;
    private float carga = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && spriteApagado != null) sr.sprite = spriteApagado;
        if (luz != null) luz.intensity = 0f;
    }

    void Update()
    {
        if (encendido || !alexDentro || rbAlex == null) return;

        bool quieto = rbAlex.linearVelocity.magnitude < velocidadQuieto;

        if (quieto)
        {
            if (carga == 0f) AudioJuego.IniciarCargaFarol();     // SONIDO
            carga += Time.deltaTime;
        }
        else
        {
            if (carga > 0f) AudioJuego.DetenerCargaFarol();      // SONIDO
            carga = 0f;
        }

        float k = Mathf.Clamp01(carga / tiempoParaEncender);

        if (luz != null)
        {
            float parpadeo = (k > 0f && Random.value < 0.35f) ? 0.3f : 1f;
            luz.intensity = intensidadLuz * k * 0.7f * parpadeo;
        }
        if (sr != null && spriteEncendido != null && spriteApagado != null)
            sr.sprite = (k > 0f && Random.value < k) ? spriteEncendido : spriteApagado;

        if (carga >= tiempoParaEncender) Encender();
    }

    void Encender()
    {
        encendido = true;
        AudioJuego.DetenerCargaFarol();                           // SONIDO
        AudioJuego.Sonar("farol");                                // SONIDO

        if (sr != null && spriteEncendido != null) sr.sprite = spriteEncendido;
        if (luz != null) luz.intensity = intensidadLuz;

        if (CorruptionManager.instancia != null)
            CorruptionManager.instancia.RegistrarPuntoSeguro(this);

        ActivarZonaSegura();
        Debug.Log("Punto seguro activado: " + name);
    }

    void ActivarZonaSegura()
    {
        if (zonaSeguraActiva || CorruptionManager.instancia == null) return;
        zonaSeguraActiva = true;
        CorruptionManager.instancia.EntrarZonaSegura();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        MovimientoAlex mov = otro.GetComponent<MovimientoAlex>();
        if (mov == null) return;

        alexDentro = true;
        rbAlex = mov.GetComponent<Rigidbody2D>();
        carga = 0f;

        if (encendido)
        {
            CorruptionManager.instancia?.RegistrarPuntoSeguro(this);
            ActivarZonaSegura();
        }
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoAlex>() == null) return;

        alexDentro = false;
        carga = 0f;

        if (!encendido)
        {
            AudioJuego.DetenerCargaFarol();                       // SONIDO
            if (luz != null) luz.intensity = 0f;
            if (sr != null && spriteApagado != null) sr.sprite = spriteApagado;
        }

        if (zonaSeguraActiva && CorruptionManager.instancia != null)
        {
            zonaSeguraActiva = false;
            CorruptionManager.instancia.SalirZonaSegura();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PosicionReaparicion, 0.3f);
    }
}
