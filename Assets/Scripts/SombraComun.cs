using System.Collections;
using UnityEngine;

public class SombraComun : MonoBehaviour
{
    [Header("Movimiento (flota y deambula)")]
    public float distanciaPatrulla = 2f;
    public float velocidad = 0.8f;
    public float alturaFlote = 0.2f;
    public float velocidadFlote = 1.5f;

    [Header("Contacto con Alex")]
    public float subidaCorrupcion = 10f;
    public float esperaEntreContactos = 1.5f;

    [Header("Calmar con LIGHT (tecla F)")]
    public float distanciaParaCalmar = 3f;
    public float bajaCorrupcion = 10f;
    public float duracionDisolver = 1.2f;
    public ParticleSystem chispas;

    [Header("Letra que suelta al calmarse (opcional)")]
    public GameObject prefabLetra;
    public char letraAtrapada = 'L';

    private SpriteRenderer sr;
    private Collider2D col;
    private Transform alex;
    private Vector3 posicionInicial;
    private float xAnterior;
    private float ultimoContacto = -99f;
    private bool calmada = false;

    // Color cálido al que cambia cuando se calma
    private readonly Color ambar = new Color(0.95f, 0.65f, 0.25f, 1f);

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        posicionInicial = transform.position;

        GameObject jugador = GameObject.FindWithTag("Player");
        if (jugador != null) alex = jugador.transform;
    }

    void Update()
    {
        if (calmada) return;

        // Deambular de lado a lado mientras flota
        float x = Mathf.PingPong(Time.time * velocidad, distanciaPatrulla * 2f) - distanciaPatrulla;
        float y = Mathf.Sin(Time.time * velocidadFlote) * alturaFlote;
        transform.position = posicionInicial + new Vector3(x, y, 0f);

        // Mirar hacia donde se mueve
        if (x > xAnterior) sr.flipX = false;
        else if (x < xAnterior) sr.flipX = true;
        xAnterior = x;

        // Calmarla con LIGHT si Alex está cerca
        if (alex != null && Input.GetKeyDown(KeyCode.F) &&
            Vector2.Distance(alex.position, transform.position) <= distanciaParaCalmar)
        {
            IntentarCalmar();
        }
    }

    void IntentarCalmar()
    {
        if (!WordManager.instancia.PalabraAprendida("LIGHT"))
        {
            Debug.Log("La sombra se acerca, pero todavía no conoces LIGHT.");
            return;
        }

        // Según el GDD, con las sombras comunes LIGHT tiene cooldown reducido: aquí no se bloquea
        CorruptionManager.instancia.BajarCorrupcion(bajaCorrupcion);
        Calmar();
    }

    // Público por si otro script quiere calmarla
    public void Calmar()
    {
        if (calmada) return;
        StartCoroutine(Disolver());
    }

    IEnumerator Disolver()
    {
        calmada = true;
        if (col != null) col.enabled = false;
        if (chispas != null) chispas.Play();

        Color inicial = sr.color;
        Vector3 escalaInicial = transform.localScale;
        float t = 0f;

        // Se entibia (de oscuro a ámbar), sube un poco y se desvanece
        while (t < duracionDisolver)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionDisolver);

            Color c = Color.Lerp(inicial, ambar, k);
            c.a = inicial.a * (1f - k);
            sr.color = c;

            transform.position += Vector3.up * (0.4f * Time.deltaTime);
            transform.localScale = escalaInicial * (1f - 0.3f * k);
            yield return null;
        }

        // Suelta la letra que tenía atrapada
        if (prefabLetra != null)
        {
            GameObject nueva = Instantiate(prefabLetra, transform.position, Quaternion.identity);
            Letra l = nueva.GetComponent<Letra>();
            if (l != null) l.letra = letraAtrapada;
        }

        Debug.Log("La sombra se calmó y se disolvió.");

        // Espera a que terminen las chispas y desaparece
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (calmada || !other.CompareTag("Player")) return;

        // Si Alex está escondido (HIDE), la sombra no lo nota
        AlexEstado estado = other.GetComponent<AlexEstado>();
        if (estado != null && estado.escondido) return;

        if (Time.time - ultimoContacto >= esperaEntreContactos)
        {
            ultimoContacto = Time.time;
            CorruptionManager.instancia.SubirCorrupcion(subidaCorrupcion);
            Debug.Log("¡Contacto con sombra! Corrupción sube.");
        }
    }
}