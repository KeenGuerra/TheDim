using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PuntoSeguro : MonoBehaviour
{
    [Header("Visual")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;
    public Light2D luz;
    public float intensidadLuz = 1f;

    [Header("Dónde reaparece Alex (relativo al farol)")]
    public Vector2 offsetReaparicion = new Vector2(1.5f, 0.5f);

    private SpriteRenderer sr;
    private bool activo = false;

    public Vector3 PosicionReaparicion => transform.position + (Vector3)offsetReaparicion;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (spriteApagado != null && sr != null) sr.sprite = spriteApagado;
        if (luz != null) luz.intensity = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CorruptionManager.instancia.RegistrarPuntoSeguro(this);
        CorruptionManager.instancia.EntrarZonaSegura();   // bajo la luz, la corrupción se detiene

        if (!activo)
        {
            activo = true;
            if (spriteEncendido != null && sr != null) sr.sprite = spriteEncendido;
            if (luz != null) luz.intensity = intensidadLuz;
            Debug.Log("Punto seguro activado: " + name);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CorruptionManager.instancia.SalirZonaSegura();
    }

    // En la Scene: celeste = dónde reaparece Alex
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + (Vector3)offsetReaparicion, 0.3f);
    }
}