using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

// JEFE DEL CAPÍTULO 1: El Susurro.
// Criatura de humo y estática que se acelera con la oscuridad.
//
// Cómo funciona la pelea:
//  1. ATAQUES: flota hacia Alex, embiste de lado a lado y hace llover gotas de estática.
//     Mientras tanto la arena se va apagando y él se vuelve más rápido.
//  2. VENTANA DE CALMA: se detiene, brilla ámbar y aparecen las letras L, G y T.
//     Si Alex las junta todas a tiempo, LIGHT lo calma: pierde una capa de oscuridad
//     y la arena se vuelve a encender. Si no, solo pierde esa oportunidad.
//  3. Al quitarle todas las capas, se calma y aparece el objeto que termina el capítulo.
//
// Ayuda: si fallas 2 ventanas seguidas, la siguiente dura más y las letras se ven más grandes.
// HIDE: mientras Alex está escondido, El Susurro no le hace daño.
public class ElSusurro : MonoBehaviour
{
    [Header("Arena (posiciones en X e Y del mundo)")]
    public float arenaIzquierda = -12f;
    public float arenaDerecha = 12f;
    public float suelo = 0f;
    [Tooltip("Altura a la que flota sobre el suelo")]
    public float alturaVuelo = 4f;
    [Tooltip("La pelea empieza cuando Alex pasa esta X")]
    public float xInicioPelea = -9f;

    [Header("Capas de oscuridad (cuántas ventanas hay que ganar)")]
    public int capas = 3;

    [Header("Ataques")]
    public float duracionAtaques = 9f;
    public float velocidadFlotar = 2.5f;
    public float velocidadEmbestida = 10f;
    public float avisoAtaque = 0.7f;
    public GameObject prefabGota;
    public int gotasPorLluvia = 16;
    [Tooltip("Desde qué altura sobre el suelo caen las gotas")]
    public float alturaGotas = 12f;
    [Tooltip("Segundos entre una gota y la siguiente")]
    public float intervaloGotas = 0.12f;
    public float danoContacto = 15f;
    public float danoGota = 10f;
    [Tooltip("Segundos sin recibir daño después de un golpe")]
    public float invulnerable = 1f;

    [Header("Ventana de calma")]
    public GameObject prefabLetra;
    public string letrasClave = "LGT";
    public float duracionVentana = 8f;
    [Tooltip("Segundos extra cuando fallaste 2 ventanas seguidas")]
    public float ayudaExtra = 4f;
    [Tooltip("Dónde aparecen las letras (opcional: si está vacío, se reparten solas)")]
    public Transform[] puntosLetras;

    [Header("La arena se apaga")]
    public Light2D luzGlobal;
    public float luzMaxima = 1f;
    public float luzMinima = 0.2f;
    public float apagadoPorSegundo = 0.03f;

    [Header("Cámara (opcional)")]
    public CinemachineCamera camara;
    public float zoomAtaque = 8f;
    public float zoomCalma = 6f;

    [Header("Al calmarlo")]
    [Tooltip("Objeto desactivado que se activa al ganar (por ejemplo la chispa de Sam con FinNivel)")]
    public GameObject activarAlCalmar;

    [Header("Visual")]
    public SpriteRenderer sprite;
    public Color colorNormal = Color.white;
    public Color colorAviso = new Color(1f, 0.35f, 0.35f, 1f);
    public Color colorCalma = new Color(1f, 0.8f, 0.4f, 1f);
    public float temblorEstatica = 0.06f;

    // ---- estado interno ----
    private Transform alex;
    private AlexEstado estadoAlex;
    private Vector3 posInicial;
    private Vector3 escalaInicial;
    private Vector3 posLogica;          // posición sin el temblor
    private int capasRestantes;
    private int fallosSeguidos = 0;
    private int letrasPendientes = 0;
    private bool peleaEmpezada = false;
    private bool calmado = false;
    private bool enVentana = false;
    private bool apagando = false;
    private float ultimoGolpe = -10f;
    private float corrupcionAnterior = 0f;
    private float zoomObjetivo;
    private readonly List<GameObject> creados = new List<GameObject>();
    private Coroutine pelea;

    void Start()
    {
        posInicial = transform.position;
        posLogica = posInicial;
        escalaInicial = transform.localScale;
        capasRestantes = capas;
        zoomObjetivo = zoomCalma;
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        MovimientoAlex mov = FindFirstObjectByType<MovimientoAlex>();
        if (mov != null)
        {
            alex = mov.transform;
            estadoAlex = mov.GetComponent<AlexEstado>();
        }

        if (luzGlobal != null) luzGlobal.intensity = luzMaxima;
        if (activarAlCalmar != null) activarAlCalmar.SetActive(false);
    }

    void Update()
    {
        if (alex == null) return;

        // ¿Empieza la pelea?
        if (!peleaEmpezada && !calmado && alex.position.x >= xInicioPelea)
        {
            peleaEmpezada = true;
            pelea = StartCoroutine(Pelea());
        }

        // La arena se apaga poco a poco mientras ataca
        if (apagando && luzGlobal != null)
            luzGlobal.intensity = Mathf.Max(luzMinima, luzGlobal.intensity - apagadoPorSegundo * Time.deltaTime);

        // Temblor de estática (solo visual)
        float t = calmado ? 0f : temblorEstatica * (1f + Oscuridad());
        transform.position = posLogica + (Vector3)(Random.insideUnitCircle * t);
        if (sprite != null && !calmado && !enVentana)
        {
            Color c = sprite.color;
            c.a = Random.value < 0.06f ? 0.55f : 1f;   // parpadeo de estática
            sprite.color = c;
        }

        // Zoom de cámara suave
        if (camara != null)
        {
            LensSettings lente = camara.Lens;
            lente.OrthographicSize = Mathf.Lerp(lente.OrthographicSize, zoomObjetivo, Time.deltaTime * 1.5f);
            camara.Lens = lente;
        }

        RevisarReinicio();
    }

    // 0 = arena encendida, 1 = arena totalmente oscura
    float Oscuridad()
    {
        if (luzGlobal == null) return 0f;
        return Mathf.InverseLerp(luzMaxima, luzMinima, luzGlobal.intensity);
    }

    // Se acelera con la oscuridad y se calma un poco con cada capa perdida
    float Velocidad(float baseVel)
    {
        float porCapas = Mathf.Lerp(0.75f, 1f, capasRestantes / (float)Mathf.Max(1, capas));
        return baseVel * (1f + Oscuridad() * 0.8f) * porCapas;
    }

    // ---------------- La pelea ----------------

    IEnumerator Pelea()
    {
        yield return new WaitForSeconds(1f);
        AudioJuego.Sonar("estatica");

        while (capasRestantes > 0)
        {
            yield return Ataques();
            yield return VentanaDeCalma();
        }
        yield return Calmarse();
    }

    IEnumerator Ataques()
    {
        zoomObjetivo = zoomAtaque;
        apagando = true;
        float fin = Time.time + duracionAtaques;
        int patron = 0;

        while (Time.time < fin)
        {
            // Flota persiguiendo a Alex un rato
            float hastaFlotar = Time.time + 1.5f;
            while (Time.time < hastaFlotar)
            {
                Vector3 objetivo = new Vector3(Mathf.Clamp(alex.position.x, arenaIzquierda, arenaDerecha), suelo + alturaVuelo, posLogica.z);
                posLogica = Vector3.MoveTowards(posLogica, objetivo, Velocidad(velocidadFlotar) * Time.deltaTime);
                yield return null;
            }

            if (patron % 2 == 0) yield return Embestida();
            else yield return Lluvia();
            patron++;
        }
        apagando = false;
    }

    IEnumerator Embestida()
    {
        // Se va al lado más lejano de Alex, a la altura del suelo
        bool desdeIzquierda = alex.position.x > (arenaIzquierda + arenaDerecha) * 0.5f;
        float xInicio = desdeIzquierda ? arenaIzquierda : arenaDerecha;
        float xFin = desdeIzquierda ? arenaDerecha : arenaIzquierda;
        Vector3 inicio = new Vector3(xInicio, suelo + 1f, posLogica.z);

        while (Vector3.Distance(posLogica, inicio) > 0.1f)
        {
            posLogica = Vector3.MoveTowards(posLogica, inicio, Velocidad(velocidadEmbestida) * 0.8f * Time.deltaTime);
            yield return null;
        }
        if (sprite != null) sprite.flipX = !desdeIzquierda;

        yield return Aviso();

        Vector3 fin = new Vector3(xFin, suelo + 1f, posLogica.z);
        while (Vector3.Distance(posLogica, fin) > 0.1f)
        {
            posLogica = Vector3.MoveTowards(posLogica, fin, Velocidad(velocidadEmbestida) * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Lluvia()
    {
        // Sube al centro de la arena
        Vector3 arriba = new Vector3((arenaIzquierda + arenaDerecha) * 0.5f, suelo + alturaVuelo + 1.5f, posLogica.z);
        while (Vector3.Distance(posLogica, arriba) > 0.1f)
        {
            posLogica = Vector3.MoveTowards(posLogica, arriba, Velocidad(velocidadFlotar) * 1.5f * Time.deltaTime);
            yield return null;
        }

        yield return Aviso();

        if (prefabGota == null) yield break;
        int cantidad = gotasPorLluvia + Mathf.RoundToInt(Oscuridad() * 3f);
        for (int i = 0; i < cantidad; i++)
        {
            // La mitad de las gotas caen cerca de Alex, la otra mitad al azar
            float x = (i % 2 == 0)
                ? Mathf.Clamp(alex.position.x + Random.Range(-2f, 2f), arenaIzquierda, arenaDerecha)
                : Random.Range(arenaIzquierda, arenaDerecha);
            GameObject g = Instantiate(prefabGota, new Vector3(x, suelo + alturaGotas + Random.Range(0f, 1.5f), 0f), Quaternion.identity);
            GotaEstatica gota = g.GetComponent<GotaEstatica>();
            if (gota != null) { gota.jefe = this; gota.suelo = suelo; }
            creados.Add(g);
            yield return new WaitForSeconds(intervaloGotas);
        }
        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator Aviso()
    {
        AudioJuego.Sonar("estatica");
        float t = 0f;
        while (t < avisoAtaque)
        {
            t += Time.deltaTime;
            if (sprite != null) sprite.color = (Mathf.FloorToInt(t * 12f) % 2 == 0) ? colorAviso : colorNormal;
            yield return null;
        }
        if (sprite != null) sprite.color = colorNormal;
    }

    IEnumerator VentanaDeCalma()
    {
        enVentana = true;
        zoomObjetivo = zoomCalma;

        // Se detiene en el centro y brilla ámbar (la "señal" de que pide luz)
        Vector3 centro = new Vector3((arenaIzquierda + arenaDerecha) * 0.5f, suelo + alturaVuelo, posLogica.z);
        while (Vector3.Distance(posLogica, centro) > 0.1f)
        {
            posLogica = Vector3.MoveTowards(posLogica, centro, velocidadFlotar * 2f * Time.deltaTime);
            yield return null;
        }

        bool ayuda = fallosSeguidos >= 2;
        CrearLetras(ayuda);
        float duracion = duracionVentana + (ayuda ? ayudaExtra : 0f);
        float fin = Time.time + duracion;

        while (Time.time < fin && letrasPendientes > 0)
        {
            if (sprite != null)
                sprite.color = Color.Lerp(colorNormal, colorCalma, 0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
            yield return null;
        }

        BorrarCreados();
        if (sprite != null) sprite.color = colorNormal;

        if (letrasPendientes <= 0)
        {
            // ¡LIGHT funcionó!
            fallosSeguidos = 0;
            capasRestantes--;
            AudioJuego.Sonar("sombra_calmar");
            yield return Destello();
            transform.localScale = escalaInicial * Mathf.Lerp(0.6f, 1f, capasRestantes / (float)Mathf.Max(1, capas));
        }
        else
        {
            // Se perdió esta oportunidad
            fallosSeguidos++;
            AudioJuego.Sonar("alerta");
        }
        enVentana = false;
    }

    void CrearLetras(bool ayuda)
    {
        letrasPendientes = 0;
        if (prefabLetra == null) return;

        for (int i = 0; i < letrasClave.Length; i++)
        {
            Vector3 pos;
            if (puntosLetras != null && i < puntosLetras.Length && puntosLetras[i] != null)
                pos = puntosLetras[i].position;
            else
            {
                // Se reparten alrededor del centro (no en toda la arena) y a la altura de Alex.
                // La del medio va abajo, en el suelo, para que no quede tapada por El Susurro.
                float centroX = (arenaIzquierda + arenaDerecha) * 0.5f;
                float ancho = Mathf.Min(arenaDerecha - arenaIzquierda - 2f, 22f);
                float k = letrasClave.Length == 1 ? 0.5f : i / (letrasClave.Length - 1f);
                float x = centroX + (k - 0.5f) * ancho;
                bool esCentro = Mathf.Abs(k - 0.5f) < 0.2f;
                float y = suelo + (esCentro ? 1f : 1.5f + (i % 2) * 2f);
                pos = new Vector3(x, y, 0f);
            }

            GameObject obj = Instantiate(prefabLetra, pos, Quaternion.identity);
            DibujarEncima(obj);
            LetraJefe l = obj.GetComponent<LetraJefe>();
            if (l != null)
            {
                l.jefe = this;
                l.Configurar(letrasClave[i], ayuda);
            }
            creados.Add(obj);
            letrasPendientes++;
        }
    }

    // Las letras siempre se dibujan delante de El Susurro
    void DibujarEncima(GameObject obj)
    {
        if (sprite == null) return;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            r.sortingLayerID = sprite.sortingLayerID;
            r.sortingOrder = sprite.sortingOrder + (r is SpriteRenderer ? 10 : 11);
        }
    }

    public void LetraRecogida()
    {
        letrasPendientes = Mathf.Max(0, letrasPendientes - 1);
        AudioJuego.Sonar("letra");
    }

    IEnumerator Destello()
    {
        // La arena se vuelve a encender de golpe
        if (luzGlobal == null) yield break;
        float desde = luzGlobal.intensity;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            luzGlobal.intensity = Mathf.Lerp(desde, luzMaxima * 1.4f, t / 0.6f);
            yield return null;
        }
        t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            luzGlobal.intensity = Mathf.Lerp(luzMaxima * 1.4f, luzMaxima, t / 0.8f);
            yield return null;
        }
    }

    IEnumerator Calmarse()
    {
        calmado = true;
        zoomObjetivo = zoomCalma;
        AudioJuego.Sonar("farol");
        if (CorruptionManager.instancia != null) CorruptionManager.instancia.BajarCorrupcion(100f);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        // Se vuelve cálido, sube y se desvanece
        Vector3 desde = posLogica;
        float t = 0f;
        while (t < 2.5f)
        {
            t += Time.deltaTime;
            float k = t / 2.5f;
            posLogica = desde + Vector3.up * k * 2f;
            if (sprite != null)
            {
                Color c = Color.Lerp(colorNormal, colorCalma, k * 2f);
                c.a = 1f - k;
                sprite.color = c;
            }
            yield return null;
        }

        if (activarAlCalmar != null) activarAlCalmar.SetActive(true);
        if (sprite != null) sprite.enabled = false;
    }

    // ---------------- Daño a Alex ----------------

    public void GolpearAlex(float cantidad)
    {
        if (calmado) return;
        if (estadoAlex != null && estadoAlex.escondido) return;      // HIDE lo protege
        if (Time.time - ultimoGolpe < invulnerable) return;
        if (CorruptionManager.instancia == null || !CorruptionManager.instancia.enabled) return;

        ultimoGolpe = Time.time;
        CorruptionManager.instancia.SubirCorrupcion(cantidad);
        AudioJuego.ContactoSombra(1f);
    }

    void OnTriggerStay2D(Collider2D otro)
    {
        if (!peleaEmpezada || enVentana) return;     // en la ventana de calma no hace daño
        if (otro.GetComponent<MovimientoAlex>() != null) GolpearAlex(danoContacto);
    }

    // ---------------- Si Alex pierde, la pelea empieza de nuevo ----------------

    void RevisarReinicio()
    {
        if (CorruptionManager.instancia == null) return;
        float actual = CorruptionManager.instancia.corrupcionActual;
        bool seReinicio = corrupcionAnterior >= 95f && actual < 60f;
        corrupcionAnterior = actual;

        if (seReinicio && peleaEmpezada && !calmado) Reiniciar();
    }

    void Reiniciar()
    {
        if (pelea != null) StopCoroutine(pelea);
        BorrarCreados();

        capasRestantes = capas;
        fallosSeguidos = 0;           // la ayuda se apaga al reiniciar el tramo
        enVentana = false;
        apagando = false;
        peleaEmpezada = false;        // vuelve a empezar cuando Alex pase xInicioPelea
        posLogica = posInicial;
        transform.localScale = escalaInicial;
        zoomObjetivo = zoomCalma;
        if (sprite != null) sprite.color = colorNormal;
        if (luzGlobal != null) luzGlobal.intensity = luzMaxima;
    }

    void BorrarCreados()
    {
        foreach (GameObject g in creados) if (g != null) Destroy(g);
        creados.Clear();
        letrasPendientes = 0;
    }

    // ---------------- Ayudas en la Scene ----------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(arenaIzquierda, suelo), new Vector3(arenaDerecha, suelo));
        Gizmos.DrawLine(new Vector3(arenaIzquierda, suelo), new Vector3(arenaIzquierda, suelo + alturaVuelo + 3f));
        Gizmos.DrawLine(new Vector3(arenaDerecha, suelo), new Vector3(arenaDerecha, suelo + alturaVuelo + 3f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(arenaIzquierda, suelo + alturaVuelo), new Vector3(arenaDerecha, suelo + alturaVuelo));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(xInicioPelea, suelo), new Vector3(xInicioPelea, suelo + 3f));
    }
}