using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ZonaOscura : MonoBehaviour
{
    [Header("Referencias (hijos de este objeto)")]
    public Light2D luz;
    public ParticleSystem chispas;

    [Header("Disolución con LIGHT")]
    public float duracionDisolver = 1.2f;
    public float intensidadMaxima = 2.5f;   // destello al disolverse
    public float intensidadFinal = 0.8f;    // luz cálida que se queda encendida
    public float bajaCorrupcion = 20f;

    [Header("Respiración de la oscuridad")]
    public float velocidadPulso = 1.5f;
    public float fuerzaPulso = 0.04f;

    private SpriteRenderer sr;
    private Collider2D colSolido;
    private bool jugadorCerca = false;
    private bool disuelta = false;
    private Vector3 escalaBase;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Buscar el collider que bloquea (el que NO es trigger)
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger) colSolido = c;
        }

        escalaBase = transform.localScale;
        if (luz != null) luz.intensity = 0f;
    }

    void Update()
    {
        if (disuelta) return;

        // La oscuridad "respira" para que se vea viva
        float pulso = Mathf.Sin(Time.time * velocidadPulso) * fuerzaPulso;
        transform.localScale = escalaBase * (1f + pulso);

        if (jugadorCerca && Input.GetKeyDown(KeyCode.F))
        {
            IntentarUsarLight();
        }
    }

    void IntentarUsarLight()
    {
        if (!WordManager.instancia.PalabraAprendida("LIGHT"))
        {
            Debug.Log("Todavía no conoces la palabra LIGHT.");
            return;
        }

        if (CorruptionManager.instancia.enCooldownLight)
        {
            Debug.Log("LIGHT está en cooldown, espera un momento.");
            return;
        }

        CorruptionManager.instancia.IniciarCooldownLight();
        CorruptionManager.instancia.BajarCorrupcion(bajaCorrupcion);
        AudioJuego.Sonar("zona");                                 // SONIDO
        StartCoroutine(Disolver());
    }

    IEnumerator Disolver()
    {
        disuelta = true;

        // Deja pasar a Alex desde el inicio del efecto
        if (colSolido != null) colSolido.enabled = false;

        if (chispas != null) chispas.Play();

        Color colorInicial = sr.color;
        float t = 0f;

        // La oscuridad se desvanece y se expande, mientras la luz se enciende
        while (t < duracionDisolver)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionDisolver);

            sr.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, colorInicial.a * (1f - k));
            transform.localScale = escalaBase * (1f + 0.15f * k);

            if (luz != null)
                luz.intensity = Mathf.Lerp(0f, intensidadMaxima, Mathf.Clamp01(k * 3f));

            yield return null;
        }

        sr.enabled = false;

        // El destello baja y queda una luz cálida permanente: la zona sigue iluminada
        t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            if (luz != null)
                luz.intensity = Mathf.Lerp(intensidadMaxima, intensidadFinal, t / 0.8f);
            yield return null;
        }

        Debug.Log("¡La zona se iluminó!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) jugadorCerca = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) jugadorCerca = false;
    }
}
