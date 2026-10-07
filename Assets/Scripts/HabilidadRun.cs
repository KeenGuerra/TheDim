using System.Collections;
using UnityEngine;

// RUN: con la tecla R, Alex corre más rápido y salta más lejos por unos segundos.
// Va en Alex, junto a MovimientoAlex.
public class HabilidadRun : MonoBehaviour
{
    public string palabra = "RUN";
    public KeyCode tecla = KeyCode.R;

    [Header("Efecto")]
    public float duracion = 2f;
    public float multiplicadorVelocidad = 1.8f;
    public float multiplicadorSalto = 1.15f;
    [Tooltip("Las animaciones van más rápido mientras corre")]
    public float velocidadAnimacion = 1.6f;

    [Header("Cooldown")]
    public float cooldown = 4f;

    [Header("Rastro visual")]
    public Color colorRastro = new Color(0.95f, 0.65f, 0.25f, 0.55f);
    public float cadaCuantoRastro = 0.05f;
    public float vidaRastro = 0.25f;
    [Tooltip("Partículas opcionales a los pies (puede quedar vacío)")]
    public ParticleSystem polvo;

    // Para el indicador del HUD
    public bool activa { get; private set; }
    public bool enCooldown { get; private set; }
    public float progresoCooldown { get; private set; } = 1f;   // 0 = recién usada, 1 = lista

    private MovimientoAlex mov;
    private SpriteRenderer sr;
    private Animator anim;

    void Start()
    {
        mov = GetComponent<MovimientoAlex>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(tecla)) return;
        if (activa || enCooldown) return;
        if (mov == null || !mov.enabled) return;   // por ejemplo, durante una escena
        if (WordManager.instancia == null || !WordManager.instancia.PalabraAprendida(palabra)) return;

        StartCoroutine(Usar());
    }

    IEnumerator Usar()
    {
        activa = true;
        AudioJuego.Sonar("run");

        mov.multiplicadorVelocidad = multiplicadorVelocidad;
        mov.multiplicadorSalto = multiplicadorSalto;
        if (anim != null) anim.speed = velocidadAnimacion;
        if (polvo != null) polvo.Play();

        float t = 0f, siguienteRastro = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            if (t >= siguienteRastro)
            {
                siguienteRastro = t + cadaCuantoRastro;
                CrearRastro();
            }
            yield return null;
        }

        mov.multiplicadorVelocidad = 1f;
        mov.multiplicadorSalto = 1f;
        if (anim != null) anim.speed = 1f;
        if (polvo != null) polvo.Stop();
        activa = false;

        // Recarga
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

    // Copia fantasma del sprite de Alex que se desvanece
    void CrearRastro()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject g = new GameObject("RastroRun");
        g.transform.position = transform.position;
        g.transform.localScale = transform.lossyScale;

        SpriteRenderer copia = g.AddComponent<SpriteRenderer>();
        copia.sprite = sr.sprite;
        copia.flipX = sr.flipX;
        copia.color = colorRastro;
        copia.sharedMaterial = sr.sharedMaterial;
        copia.sortingLayerID = sr.sortingLayerID;
        copia.sortingOrder = sr.sortingOrder - 1;

        StartCoroutine(Desvanecer(copia));
    }

    IEnumerator Desvanecer(SpriteRenderer copia)
    {
        float t = 0f;
        Color c = copia.color;
        while (t < vidaRastro && copia != null)
        {
            t += Time.deltaTime;
            copia.color = new Color(c.r, c.g, c.b, c.a * (1f - t / vidaRastro));
            yield return null;
        }
        if (copia != null) Destroy(copia.gameObject);
    }

    // Si se desactiva (por ejemplo, al cortarse la conexión), vuelve todo a la normalidad
    void OnDisable()
    {
        if (mov != null) { mov.multiplicadorVelocidad = 1f; mov.multiplicadorSalto = 1f; }
        if (anim != null) anim.speed = 1f;
        activa = false;
        enCooldown = false;
        progresoCooldown = 1f;
    }
}
