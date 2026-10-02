using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Linterna de Alex: apagada al inicio, se enciende al aprender LIGHT,
// y con F lanza un haz de luz con un destello.
public class LinternaLight : MonoBehaviour
{
    [Header("Haz de luz")]
    public GameObject prefabHaz;
    public float tiempoEntreHaces = 0.6f;

    [Header("Posición de la linterna en cada animación (mirando a la derecha)")]
    public Vector2 offsetIdle = new Vector2(0.9f, 1.6f);
    public Vector2 offsetRun = new Vector2(1.0f, 1.7f);
    public Vector2 offsetJump = new Vector2(0.9f, 2.0f);
    public float suavizado = 25f;

    [Header("Luz de la linterna")]
    public Light2D luzLinterna;
    public float intensidadTenue = 0.5f;
    public float intensidadDestello = 2.5f;
    public float duracionDestello = 0.25f;

    private SpriteRenderer sr;
    private Animator anim;
    private float ultimoHaz = -99f;
    private bool encendida = false;
    private Coroutine destello;
    private Vector2 offsetActual;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        offsetActual = offsetIdle;
        if (luzLinterna != null) luzLinterna.intensity = 0f;
    }

    void Update()
    {
        bool aprendida = WordManager.instancia != null && WordManager.instancia.PalabraAprendida("LIGHT");

        if (aprendida && !encendida)
        {
            encendida = true;
            StartCoroutine(EncenderPorPrimeraVez());
        }

        offsetActual = Vector2.Lerp(offsetActual, OffsetSegunAnimacion(), Time.deltaTime * suavizado);
        ColocarLuz();

        if (!Input.GetKeyDown(KeyCode.F)) return;
        if (!aprendida || prefabHaz == null) return;
        if (Time.time - ultimoHaz < tiempoEntreHaces) return;

        ultimoHaz = Time.time;
        LanzarHaz();
    }

    Vector2 OffsetSegunAnimacion()
    {
        if (anim == null) return offsetIdle;
        AnimatorStateInfo estado = anim.GetCurrentAnimatorStateInfo(0);
        if (estado.IsName("Alex_Jump")) return offsetJump;
        if (estado.IsName("Alex_Run")) return offsetRun;
        return offsetIdle;
    }

    Vector3 PuntoLinterna()
    {
        bool izquierda = MirandoIzquierda();
        return transform.position + new Vector3(izquierda ? -offsetActual.x : offsetActual.x, offsetActual.y, 0f);
    }

    void LanzarHaz()
    {
        offsetActual = OffsetSegunAnimacion();

        Vector2 dir = MirandoIzquierda() ? Vector2.left : Vector2.right;
        GameObject haz = Instantiate(prefabHaz, PuntoLinterna(), Quaternion.identity);
        HazLuz h = haz.GetComponent<HazLuz>();
        if (h != null) h.Lanzar(dir);
        AudioJuego.Sonar("haz");                                  // SONIDO

        if (destello != null) StopCoroutine(destello);
        destello = StartCoroutine(Destello());
    }

    void ColocarLuz()
    {
        if (luzLinterna == null) return;
        luzLinterna.transform.position = PuntoLinterna();
        luzLinterna.transform.rotation = Quaternion.Euler(0f, 0f, MirandoIzquierda() ? 90f : -90f);
    }

    bool MirandoIzquierda()
    {
        return sr != null && sr.flipX;
    }

    IEnumerator EncenderPorPrimeraVez()
    {
        if (luzLinterna == null) yield break;
        AudioJuego.Sonar("linterna");                             // SONIDO

        float[] pausas = { 0.08f, 0.12f, 0.06f, 0.2f, 0.07f, 0.1f };
        bool on = false;
        foreach (float p in pausas)
        {
            on = !on;
            luzLinterna.intensity = on ? intensidadTenue * 1.6f : 0f;
            yield return new WaitForSeconds(p);
        }
        luzLinterna.intensity = intensidadTenue;
    }

    IEnumerator Destello()
    {
        if (luzLinterna == null) yield break;
        float t = 0f;
        while (t < duracionDestello)
        {
            t += Time.deltaTime;
            luzLinterna.intensity = Mathf.Lerp(intensidadDestello, intensidadTenue, t / duracionDestello);
            yield return null;
        }
        luzLinterna.intensity = intensidadTenue;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(p + (Vector3)offsetIdle, 0.12f);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p + (Vector3)offsetRun, 0.12f);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(p + (Vector3)offsetJump, 0.12f);
    }
}
