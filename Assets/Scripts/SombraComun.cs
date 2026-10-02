using System.Collections;
using UnityEngine;
using TMPro;

public class SombraComun : MonoBehaviour
{
    [Header("Patrulla")]
    public float velocidad = 1.5f;
    public float distanciaPatrulla = 3f;
    public float alturaFlotar = 0.25f;
    public float velocidadFlotar = 2f;

    [Header("Calmar con F estando cerca (0 = solo con el haz de luz)")]
    public float distanciaCalmar = 0f;
    public float corrupcionQueBaja = 10f;

    [Header("Contacto: la corrupción sube más rápido mientras te toca")]
    public float corrupcionPorSegundo = 8f;
    public float aceleracion = 3f;

    [Header("Efectos")]
    public ParticleSystem chispas;
    public Color colorCalmada = new Color(0.95f, 0.65f, 0.25f, 1f);
    public float duracionCalma = 1.2f;

    [Header("Letra atrapada (opcional)")]
    public GameObject prefabLetra;
    public char letraAtrapada = ' ';

    private Transform alex;
    private AlexEstado estadoAlex;
    private SpriteRenderer sr;
    private Vector3 inicio;
    private bool calmada = false;
    private bool tocandoAlex = false;
    private float intensidadContacto = 0f;

    void Start()
    {
        inicio = transform.position;
        sr = GetComponentInChildren<SpriteRenderer>();

        MovimientoAlex mov = FindFirstObjectByType<MovimientoAlex>();
        if (mov != null)
        {
            alex = mov.transform;
            estadoAlex = mov.GetComponent<AlexEstado>();
        }
    }

    void Update()
    {
        if (calmada) return;

        Patrullar();
        ActualizarContacto();

        if (distanciaCalmar > 0f && Input.GetKeyDown(KeyCode.F) && alex != null)
        {
            if (Vector2.Distance(alex.position, transform.position) <= distanciaCalmar &&
                WordManager.instancia != null && WordManager.instancia.PalabraAprendida("LIGHT"))
                Calmar();
        }
    }

    void Patrullar()
    {
        float x = inicio.x + Mathf.Sin(Time.time * velocidad / Mathf.Max(0.1f, distanciaPatrulla)) * distanciaPatrulla;
        float y = inicio.y + Mathf.Sin(Time.time * velocidadFlotar) * alturaFlotar;

        if (sr != null) sr.flipX = x < transform.position.x;
        transform.position = new Vector3(x, y, inicio.z);
    }

    void ActualizarContacto()
    {
        bool escondido = estadoAlex != null && estadoAlex.escondido;
        float objetivo = (tocandoAlex && !escondido) ? 1f : 0f;

        intensidadContacto = Mathf.MoveTowards(intensidadContacto, objetivo, Time.deltaTime * aceleracion);
        AudioJuego.ContactoSombra(intensidadContacto);            // SONIDO

        if (intensidadContacto > 0f && CorruptionManager.instancia != null && CorruptionManager.instancia.enabled)
            CorruptionManager.instancia.SubirCorrupcion(corrupcionPorSegundo * intensidadContacto * Time.deltaTime);
    }

    public void Calmar()
    {
        if (calmada) return;
        calmada = true;
        tocandoAlex = false;

        if (CorruptionManager.instancia != null)
            CorruptionManager.instancia.BajarCorrupcion(corrupcionQueBaja);
        AudioJuego.Sonar("sombra_calmar");                        // SONIDO

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        StartCoroutine(Disolver());
    }

    IEnumerator Disolver()
    {
        if (chispas != null) chispas.Play();

        Color colorInicial = sr != null ? sr.color : Color.white;
        Vector3 posInicial = transform.position;
        Vector3 escalaInicial = transform.localScale;

        float t = 0f;
        while (t < duracionCalma)
        {
            t += Time.deltaTime;
            float k = t / duracionCalma;

            if (sr != null)
            {
                Color c = Color.Lerp(colorInicial, colorCalmada, Mathf.Clamp01(k * 2f));
                c.a = 1f - Mathf.Clamp01((k - 0.5f) * 2f);
                sr.color = c;
            }
            transform.position = posInicial + Vector3.up * k * 1.5f;
            transform.localScale = escalaInicial * (1f + k * 0.3f);
            yield return null;
        }

        SoltarLetra(posInicial);

        if (chispas != null && chispas.transform.IsChildOf(transform))
        {
            chispas.transform.SetParent(null);
            Destroy(chispas.gameObject, 2f);
        }
        Destroy(gameObject);
    }

    void SoltarLetra(Vector3 posicion)
    {
        if (prefabLetra == null || letraAtrapada == ' ') return;

        GameObject obj = Instantiate(prefabLetra, posicion, Quaternion.identity);
        Letra l = obj.GetComponent<Letra>();
        if (l != null) l.letra = letraAtrapada;
        TextMeshPro texto = obj.GetComponentInChildren<TextMeshPro>();
        if (texto != null) texto.text = letraAtrapada.ToString();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoAlex>() != null) tocandoAlex = true;
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoAlex>() != null) tocandoAlex = false;
    }

    void OnDrawGizmosSelected()
    {
        if (distanciaCalmar > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, distanciaCalmar);
        }
        Vector3 c = Application.isPlaying ? inicio : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(c + Vector3.left * distanciaPatrulla, c + Vector3.right * distanciaPatrulla);
    }
}
