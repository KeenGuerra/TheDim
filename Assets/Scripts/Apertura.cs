using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class Apertura : MonoBehaviour
{
    [Header("Referencias")]
    public Transform alex;
    [Tooltip("Imagen negra de pantalla completa, solo para la apertura (en el Canvas)")]
    public Image negroInicio;

    [Header("Ventana de la casa de Sam")]
    public SpriteRenderer ventana;
    public Sprite ventanaEncendida;
    public Sprite ventanaApagada;
    public Light2D luzVentana;

    [Header("Chispa (la conexión con Sam)")]
    public SpriteRenderer chispa;
    public Light2D luzChispa;
    [Tooltip("Hacia dónde vuela la chispa: el camino que debe seguir el jugador")]
    public Transform destinoChispa;
    public float duracionViaje = 2.5f;

    [Header("Tiempos")]
    public float duracionFundido = 1.5f;
    public float esperaAntesDelParpadeo = 1.2f;

    [Header("Opciones")]
    [Tooltip("Enter o Escape saltan la apertura")]
    public bool permitirSaltar = true;

    private MovimientoAlex movimiento;
    private Rigidbody2D rb;
    private Animator anim;
    private float intensidadVentana = 1f;
    private bool terminada = false;
    private Coroutine rutina;

    void Start()
    {
        if (alex == null)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador != null) alex = jugador.transform;
        }

        if (alex != null)
        {
            movimiento = alex.GetComponent<MovimientoAlex>();
            rb = alex.GetComponent<Rigidbody2D>();
            anim = alex.GetComponent<Animator>();
        }

        if (luzVentana != null) intensidadVentana = luzVentana.intensity;
        PonerVentana(true);

        if (chispa != null) chispa.enabled = false;
        if (luzChispa != null) luzChispa.intensity = 0f;

        if (negroInicio != null)
        {
            negroInicio.gameObject.SetActive(true);
            negroInicio.raycastTarget = false;
            PonerAlpha(negroInicio, 1f);
        }

        Bloquear(true);
        rutina = StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (terminada || !permitirSaltar) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (rutina != null) StopCoroutine(rutina);
            Terminar();
        }
    }

    IEnumerator Secuencia()
    {
        // 1. La imagen aparece desde negro
        yield return Fundir(1f, 0f);

        // 2. Alex mira hacia la ventana de Sam
        if (ventana != null && alex != null)
        {
            SpriteRenderer srAlex = alex.GetComponent<SpriteRenderer>();
            if (srAlex != null) srAlex.flipX = ventana.transform.position.x < alex.position.x;
        }

        yield return new WaitForSeconds(esperaAntesDelParpadeo);

        // 3. La luz de la ventana parpadea... y se apaga
        float[] pausas = { 0.15f, 0.1f, 0.25f, 0.08f, 0.35f, 0.1f, 0.12f };
        bool encendida = true;
        foreach (float p in pausas)
        {
            encendida = !encendida;
            PonerVentana(encendida);
            yield return new WaitForSeconds(p);
        }
        PonerVentana(false);

        yield return new WaitForSeconds(0.7f);

        // 4. Una chispa sale de la ventana y se aleja: el camino a seguir
        if (chispa != null && destinoChispa != null)
        {
            Vector3 inicio = ventana != null ? ventana.transform.position : chispa.transform.position;
            Vector3 fin = destinoChispa.position;
            chispa.enabled = true;

            float t = 0f;
            while (t < duracionViaje)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duracionViaje);

                // Vuela con una ondulación suave
                chispa.transform.position = Vector3.Lerp(inicio, fin, k)
                                            + Vector3.up * Mathf.Sin(k * Mathf.PI * 3f) * 0.4f;

                // Aparece rápido y se desvanece al final
                float alpha = Mathf.Clamp01(k / 0.1f) * Mathf.Clamp01((1f - k) / 0.3f);
                Color c = chispa.color;
                c.a = alpha;
                chispa.color = c;
                if (luzChispa != null) luzChispa.intensity = alpha * 1.2f;

                yield return null;
            }

            chispa.enabled = false;
            if (luzChispa != null) luzChispa.intensity = 0f;
        }

        yield return new WaitForSeconds(0.3f);

        // 5. El jugador toma el control
        Terminar();
    }

    void Terminar()
    {
        if (terminada) return;
        terminada = true;

        if (negroInicio != null)
        {
            PonerAlpha(negroInicio, 0f);
            negroInicio.gameObject.SetActive(false);
        }

        PonerVentana(false);
        if (chispa != null) chispa.enabled = false;
        if (luzChispa != null) luzChispa.intensity = 0f;

        Bloquear(false);
    }

    void Bloquear(bool bloquear)
    {
        if (movimiento != null) movimiento.enabled = !bloquear;

        if (bloquear)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) anim.Play("Alex_Idle");
        }

        // La corrupción no sube durante la apertura
        if (CorruptionManager.instancia != null) CorruptionManager.instancia.enabled = !bloquear;
    }

    void PonerVentana(bool encendida)
    {
        if (ventana != null)
        {
            Sprite s = encendida ? ventanaEncendida : ventanaApagada;
            if (s != null) ventana.sprite = s;
        }
        if (luzVentana != null) luzVentana.intensity = encendida ? intensidadVentana : 0f;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        if (negroInicio == null) yield break;

        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.deltaTime;
            PonerAlpha(negroInicio, Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        PonerAlpha(negroInicio, hasta);
    }

    void PonerAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}
