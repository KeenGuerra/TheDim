using System.Collections;
using UnityEngine;

// HIDE: con la tecla H, Alex se vuelve semitransparente unos segundos
// y las sombras no lo ven ni le suben la corrupción.
// Va en Alex. (Otros scripts leen "escondido".)
public class AlexEstado : MonoBehaviour
{
    public string palabra = "HIDE";
    public KeyCode tecla = KeyCode.H;

    [Header("Efecto")]
    public float duracionHide = 3f;
    [Range(0f, 1f)] public float transparenciaOculto = 0.35f;
    public Color tinteOculto = new Color(0.7f, 0.6f, 1f, 1f);   // morado suave
    [Tooltip("Velocidad mientras está oculto (1 = normal). Un poco más lento se siente sigiloso.")]
    public float multiplicadorVelocidad = 0.85f;

    [Header("Cooldown")]
    public float cooldown = 5f;

    // Lo leen las sombras y el indicador del HUD
    public bool escondido { get; private set; }
    public bool enCooldown { get; private set; }
    public float progresoCooldown { get; private set; } = 1f;

    private SpriteRenderer sr;
    private MovimientoAlex mov;
    private Color colorOriginal;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        mov = GetComponent<MovimientoAlex>();
        if (sr != null) colorOriginal = sr.color;
    }

    void Update()
    {
        if (!Input.GetKeyDown(tecla)) return;
        if (escondido || enCooldown) return;
        if (mov != null && !mov.enabled) return;
        if (WordManager.instancia == null || !WordManager.instancia.PalabraAprendida(palabra)) return;

        ActivarHide();
    }

    public void ActivarHide()
    {
        if (escondido || enCooldown) return;
        StartCoroutine(Ocultar());
    }

    IEnumerator Ocultar()
    {
        escondido = true;
        AudioJuego.Sonar("hide");
        if (mov != null) mov.multiplicadorVelocidad = multiplicadorVelocidad;

        float t = 0f;
        while (t < duracionHide)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                // Parpadea en el último segundo para avisar que se acaba
                bool avisando = duracionHide - t < 1f && Mathf.Repeat(t * 8f, 1f) < 0.5f;
                Color c = tinteOculto;
                c.a = avisando ? 0.75f : transparenciaOculto;
                sr.color = c;
            }
            yield return null;
        }

        if (sr != null) sr.color = colorOriginal;
        if (mov != null && mov.multiplicadorVelocidad == multiplicadorVelocidad) mov.multiplicadorVelocidad = 1f;
        escondido = false;

        enCooldown = true;
        t = 0f;
        while (t < cooldown)
        {
            t += Time.deltaTime;
            progresoCooldown = t / cooldown;
            yield return null;
        }
        progresoCooldown = 1f;
        enCooldown = false;
    }

    void OnDisable()
    {
        if (sr != null && colorOriginal.a > 0f) sr.color = colorOriginal;
        escondido = false;
        enCooldown = false;
        progresoCooldown = 1f;
    }
}
