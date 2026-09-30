using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CorruptionManager : MonoBehaviour
{
    public static CorruptionManager instancia;

    [Header("Corrupción")]
    [Range(0, 100)] public float corrupcionActual = 0f;
    public float subidaPorSegundo = 1f;
    public float corrupcionAlReaparecer = 27f;

    [Header("Cooldown de LIGHT")]
    public bool enCooldownLight = false;
    public float duracionCooldownLight = 4f;

    [Header("Reaparición")]
    public Transform alex;
    [Tooltip("Imagen negra que cubre toda la pantalla (en el Canvas)")]
    public Image fundido;
    public float duracionFundido = 0.8f;

    [Header("Corte de la conexión con Sam")]
    public Image vineta;
    public Image destello;
    public AudioSource estatica;
    public float duracionCierre = 1.6f;
    public float alturaDestello = 5f;   // qué tan arriba de Alex aparece el destello
    [Tooltip("Desde qué corrupción empiezan a oscurecerse los bordes como aviso")]
    [Range(0, 100)] public float avisoDesde = 70f;
    [Range(0f, 1f)] public float alphaAvisoMaximo = 0.5f;

    private Vector3 puntoReaparicion;
    private bool enCorte = false;
    private int zonasSeguras = 0;   // en cuántas luces de farol está Alex ahora

    public bool EnZonaSegura => zonasSeguras > 0;

    void Awake()
    {
        if (instancia == null) instancia = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (alex == null)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador != null) alex = jugador.transform;
        }

        // Si todavía no tocó ningún farol, reaparece donde empezó
        if (alex != null) puntoReaparicion = alex.position;

        PrepararImagen(fundido);
        PrepararImagen(vineta);
        PrepararImagen(destello);
    }

    void Update()
    {
        ActualizarAviso();

        // Durante el corte o bajo la luz de un farol, la corrupción no sube
        if (enCorte || EnZonaSegura) return;

        corrupcionActual += subidaPorSegundo * Time.deltaTime;
        corrupcionActual = Mathf.Clamp(corrupcionActual, 0f, 100f);

        if (corrupcionActual >= 100f)
            StartCoroutine(CorteConexion());
    }

    // Los bordes se oscurecen de a poco cuando la corrupción está alta
    void ActualizarAviso()
    {
        if (vineta == null || enCorte) return;
        float k = Mathf.InverseLerp(avisoDesde, 100f, corrupcionActual);
        PonerAlpha(vineta, k * alphaAvisoMaximo);
    }

    // ---------- Puntos seguros ----------

    public void RegistrarPuntoSeguro(PuntoSeguro punto)
    {
        puntoReaparicion = punto.PosicionReaparicion;
    }

    public void EntrarZonaSegura()
    {
        zonasSeguras++;
    }

    public void SalirZonaSegura()
    {
        zonasSeguras = Mathf.Max(0, zonasSeguras - 1);
    }

    // ---------- Corte de la conexión y reaparición ----------

    IEnumerator CorteConexion()
    {
        enCorte = true;
        Debug.Log("Corrupción al máximo: se corta la conexión con Sam.");

        // Alex deja de responder mientras dura el corte
        MovimientoAlex movimiento = alex.GetComponent<MovimientoAlex>();
        Rigidbody2D rb = alex.GetComponent<Rigidbody2D>();
        if (movimiento != null) movimiento.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 1. Los bordes se cierran, el destello titila y la estática crece
        if (estatica != null)
        {
            estatica.volume = 0f;
            estatica.Play();
        }

        float alphaInicial = vineta != null ? vineta.color.a : 0f;
        float t = 0f;
        while (t < duracionCierre)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionCierre);

            if (vineta != null) PonerAlpha(vineta, Mathf.Lerp(alphaInicial, 1f, k));

            if (destello != null)
            {
                // Sigue a Alex en la pantalla
                if (Camera.main != null)
                    destello.rectTransform.position =
                        Camera.main.WorldToScreenPoint(alex.position + Vector3.up * alturaDestello);

                // Se encoge y titila cada vez más
                float titileo = (Random.value < k * 0.5f) ? 0.2f : 1f;
                PonerAlpha(destello, titileo);
                destello.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.4f, k);
            }

            if (estatica != null) estatica.volume = Mathf.Lerp(0.15f, 0.8f, k);

            yield return null;
        }

        // 2. El destello se apaga y la estática se corta de golpe
        if (destello != null) PonerAlpha(destello, 0f);
        if (estatica != null) estatica.Stop();

        yield return new WaitForSeconds(0.5f);   // silencio

        // 3. Fundido a negro
        yield return Fundir(0f, 1f);

        // Reaparece en el último punto seguro, conservando su vocabulario
        alex.position = puntoReaparicion;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        corrupcionActual = corrupcionAlReaparecer;
        if (vineta != null) PonerAlpha(vineta, 0f);

        yield return new WaitForSeconds(0.4f);

        // 4. Vuelve la imagen
        yield return Fundir(1f, 0f);

        if (movimiento != null) movimiento.enabled = true;
        enCorte = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        if (fundido == null) yield break;

        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.deltaTime;
            PonerAlpha(fundido, Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        PonerAlpha(fundido, hasta);
    }

    void PrepararImagen(Image img)
    {
        if (img == null) return;
        img.raycastTarget = false;
        PonerAlpha(img, 0f);
    }

    void PonerAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    // ---------- Subir y bajar corrupción ----------

    public void BajarCorrupcion(float cantidad)
    {
        corrupcionActual = Mathf.Clamp(corrupcionActual - cantidad, 0f, 100f);
    }

    public void SubirCorrupcion(float cantidad)
    {
        if (enCorte) return;
        corrupcionActual = Mathf.Clamp(corrupcionActual + cantidad, 0f, 100f);
    }

    // ---------- Cooldown de LIGHT ----------

    public void IniciarCooldownLight()
    {
        if (!enCooldownLight)
        {
            enCooldownLight = true;
            Invoke(nameof(TerminarCooldownLight), duracionCooldownLight);
        }
    }

    private void TerminarCooldownLight()
    {
        enCooldownLight = false;
    }
}