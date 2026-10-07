using UnityEngine;

// Maneja toda la música y los efectos del nivel.
// Va en un objeto vacío llamado "AudioJuego".
// Desde cualquier script se usa así:  AudioJuego.Sonar("letra");
public class AudioJuego : MonoBehaviour
{
    public static AudioJuego instancia;

    [Header("Música (las dos pistas suenan juntas y se mezclan según la corrupción)")]
    public AudioClip musica;
    public AudioClip musicaCorrupta;
    [Range(0f, 1f)] public float volumenMusica = 0.45f;
    [Tooltip("Desde qué corrupción empieza a escucharse la versión corrupta")]
    public float corrupcionEmpieza = 40f;
    [Tooltip("Con qué corrupción ya solo se escucha la versión corrupta")]
    public float corrupcionTotal = 90f;

    [Header("Efectos")]
    [Range(0f, 1f)] public float volumenEfectos = 0.8f;
    public AudioClip letra;
    public AudioClip letrero;
    public AudioClip linterna;
    public AudioClip haz;
    public AudioClip zona;
    public AudioClip sombraCalmar;
    public AudioClip farolCarga;
    public AudioClip farol;
    public AudioClip salto;
    public AudioClip dobleSalto;
    public AudioClip run;
    public AudioClip tabla;
    public AudioClip caida;
    public AudioClip hide;
    public AudioClip alerta;
    public AudioClip estatica;

    [Header("Sonido continuo al tocar una sombra")]
    public AudioClip sombraContacto;
    [Range(0f, 1f)] public float volumenContacto = 0.6f;

    private AudioSource fuenteMusica;
    private AudioSource fuenteCorrupta;
    private AudioSource fuenteEfectos;
    private AudioSource fuenteContacto;
    private AudioSource fuenteFarol;

    private float mezcla = 0f;               // 0 = música normal, 1 = corrupta
    private float contactoEsteFrame = 0f;
    private float contactoActual = 0f;

    void Awake()
    {
        instancia = this;

        fuenteMusica = CrearFuente(true);
        fuenteCorrupta = CrearFuente(true);
        fuenteEfectos = CrearFuente(false);
        fuenteContacto = CrearFuente(true);
        fuenteFarol = CrearFuente(false);
    }

    void Start()
    {
        // Las dos músicas arrancan exactamente al mismo tiempo para que la mezcla sea suave
        double inicio = AudioSettings.dspTime + 0.1;
        if (musica != null)
        {
            fuenteMusica.clip = musica;
            fuenteMusica.PlayScheduled(inicio);
        }
        if (musicaCorrupta != null)
        {
            fuenteCorrupta.clip = musicaCorrupta;
            fuenteCorrupta.volume = 0f;
            fuenteCorrupta.PlayScheduled(inicio);
        }
        if (sombraContacto != null)
        {
            fuenteContacto.clip = sombraContacto;
            fuenteContacto.volume = 0f;
            fuenteContacto.Play();
        }
    }

    void Update()
    {
        // --- Mezcla de música según la corrupción ---
        float objetivo = 0f;
        if (CorruptionManager.instancia != null)
            objetivo = Mathf.InverseLerp(corrupcionEmpieza, corrupcionTotal, CorruptionManager.instancia.corrupcionActual);

        mezcla = Mathf.MoveTowards(mezcla, objetivo, Time.deltaTime * 0.5f);
        fuenteMusica.volume = volumenMusica * Mathf.Cos(mezcla * Mathf.PI * 0.5f);
        fuenteCorrupta.volume = volumenMusica * Mathf.Sin(mezcla * Mathf.PI * 0.5f);
    }

    void LateUpdate()
    {
        // --- Zumbido de las sombras: tan fuerte como la sombra que más te esté tocando ---
        contactoActual = Mathf.MoveTowards(contactoActual, contactoEsteFrame, Time.deltaTime * 3f);
        fuenteContacto.volume = volumenContacto * contactoActual;
        contactoEsteFrame = 0f;
    }

    AudioSource CrearFuente(bool enBucle)
    {
        AudioSource f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.loop = enBucle;
        f.spatialBlend = 0f;   // sonido 2D
        return f;
    }

    // ----------------- Para llamar desde otros scripts -----------------

    public static void Sonar(string nombre, float volumen = 1f)
    {
        if (instancia == null) return;
        AudioClip clip = instancia.Buscar(nombre);
        if (clip == null) return;

        // Pequeña variación de tono para que no suene siempre idéntico
        instancia.fuenteEfectos.pitch = Random.Range(0.96f, 1.04f);
        instancia.fuenteEfectos.PlayOneShot(clip, instancia.volumenEfectos * volumen);
    }

    // Las sombras lo llaman cada frame con un valor de 0 a 1
    public static void ContactoSombra(float intensidad)
    {
        if (instancia == null) return;
        instancia.contactoEsteFrame = Mathf.Max(instancia.contactoEsteFrame, intensidad);
    }

    public static void IniciarCargaFarol()
    {
        if (instancia == null || instancia.farolCarga == null) return;
        instancia.fuenteFarol.clip = instancia.farolCarga;
        instancia.fuenteFarol.volume = instancia.volumenEfectos * 0.6f;
        instancia.fuenteFarol.Play();
    }

    public static void DetenerCargaFarol()
    {
        if (instancia == null) return;
        instancia.fuenteFarol.Stop();
    }

    AudioClip Buscar(string nombre)
    {
        switch (nombre)
        {
            case "letra": return letra;
            case "letrero": return letrero;
            case "linterna": return linterna;
            case "haz": return haz;
            case "zona": return zona;
            case "sombra_calmar": return sombraCalmar;
            case "farol": return farol;
            case "salto": return salto;
            case "doble_salto": return dobleSalto;
            case "run": return run;
            case "tabla": return tabla;
            case "caida": return caida;
            case "hide": return hide;
            case "alerta": return alerta;
            case "estatica": return estatica;
        }
        Debug.LogWarning("AudioJuego: no hay sonido llamado " + nombre);
        return null;
    }
}
