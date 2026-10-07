using UnityEngine;

// Sombra Vigilante (Nivel 3).
// - Patrulla de lado a lado mirando hacia adelante.
// - Si VE a Alex (dentro de su rango de visión y Alex NO está escondido), se alerta y lo persigue.
// - El haz de luz NO la calma: la única forma de pasar es usar HIDE (tecla H).
// - Si te toca, la corrupción sube rápido.
// Va en el objeto de la sombra (con un Collider 2D marcado como "Is Trigger").
public class SombraVigilante : MonoBehaviour
{
    [Header("Patrulla")]
    public float velocidadPatrulla = 1.2f;
    [Tooltip("Cuánto camina hacia cada lado desde donde la pusiste")]
    public float distanciaPatrulla = 3f;
    [Tooltip("Segundos que se queda mirando al llegar a cada extremo")]
    public float esperaEnBorde = 0.8f;
    public float alturaFlotar = 0.2f;
    public float velocidadFlotar = 2f;

    [Header("Visión (rectángulo delante de ella)")]
    public float distanciaVision = 5f;
    public float altoVision = 2f;
    [Tooltip("Capa de paredes/plataformas que tapan la vista (opcional, puede quedar en Nothing)")]
    public LayerMask capaParedes;

    [Header("Persecución")]
    public float velocidadPersecucion = 3.2f;
    [Tooltip("Cuánto puede alejarse de su zona de patrulla mientras persigue")]
    public float margenPersecucion = 2f;
    [Tooltip("Segundos que se queda quieta con el '!' antes de perseguir")]
    public float tiempoAlerta = 0.4f;
    [Tooltip("Segundos que busca a Alex después de perderlo de vista")]
    public float tiempoBuscar = 1.5f;

    [Header("Contacto")]
    public float corrupcionPorSegundo = 20f;
    public float aceleracion = 4f;

    [Header("Pruebas")]
    [Tooltip("Escribe en la Console cuando cambia de estado")]
    public bool mostrarMensajes = true;

    [Header("Visual")]
    [Tooltip("Marca si tu dibujo mira a la IZQUIERDA en la imagen original")]
    public bool spriteMiraIzquierda = false;
    [Tooltip("Objeto hijo con el '!' (opcional)")]
    public GameObject iconoAlerta;
    [Tooltip("Sprite del cono de visión (opcional)")]
    public SpriteRenderer conoVision;
    public Color colorConoCalma = new Color(1f, 1f, 0.6f, 0.25f);
    public Color colorConoAlerta = new Color(1f, 0.25f, 0.2f, 0.4f);
    public Color colorPersiguiendo = new Color(1f, 0.55f, 0.55f, 1f);

    private enum Estado { Patrulla, Alerta, Persigue, Busca }
    private Estado estado = Estado.Patrulla;

    private Transform alex;
    private AlexEstado estadoAlex;
    private Collider2D colAlex;
    private SpriteRenderer sr;
    private Color colorNormal = Color.white;
    private Vector3 inicio;
    private float xActual;
    private int direccion = 1;          // 1 = derecha, -1 = izquierda
    private float temporizador = 0f;
    private bool tocandoAlex = false;
    private float intensidadContacto = 0f;

    void Start()
    {
        inicio = transform.position;
        xActual = inicio.x;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == conoVision) sr = null;
        if (sr != null) colorNormal = sr.color;

        MovimientoAlex mov = FindFirstObjectByType<MovimientoAlex>();
        if (mov != null)
        {
            alex = mov.transform;
            estadoAlex = mov.GetComponent<AlexEstado>();
            colAlex = mov.GetComponent<Collider2D>();
            if (colAlex == null) colAlex = mov.GetComponentInChildren<Collider2D>();
        }

        // Si olvidaste conectar el "!", lo busca entre sus hijos por nombre
        if (iconoAlerta == null)
        {
            Transform t = transform.Find("Alerta");
            if (t != null) iconoAlerta = t.gameObject;
        }
        if (conoVision == null)
        {
            Transform t = transform.Find("Cono");
            if (t != null) conoVision = t.GetComponent<SpriteRenderer>();
        }

        // El "!" siempre se dibuja encima de la sombra
        if (iconoAlerta != null)
        {
            SpriteRenderer srAlerta = iconoAlerta.GetComponent<SpriteRenderer>();
            if (srAlerta != null && sr != null)
            {
                srAlerta.sortingLayerID = sr.sortingLayerID;
                srAlerta.sortingOrder = sr.sortingOrder + 5;
            }
            iconoAlerta.SetActive(false);
        }
        else Debug.LogWarning(name + ": no encuentro el '!'. Crea un hijo llamado Alerta o arrástralo a Icono Alerta.");
    }

    void Update()
    {
        bool loVe = PuedeVerAlex();

        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                if (loVe) CambiarA(Estado.Alerta);
                break;

            case Estado.Alerta:
                temporizador -= Time.deltaTime;
                if (temporizador <= 0f) CambiarA(loVe ? Estado.Persigue : Estado.Busca);
                break;

            case Estado.Persigue:
                if (loVe) Perseguir();
                else CambiarA(Estado.Busca);
                break;

            case Estado.Busca:
                temporizador -= Time.deltaTime;
                if (loVe) CambiarA(Estado.Persigue);
                else if (temporizador <= 0f) CambiarA(Estado.Patrulla);
                break;
        }

        // Flotar arriba y abajo siempre
        float y = inicio.y + Mathf.Sin(Time.time * velocidadFlotar) * alturaFlotar;
        transform.position = new Vector3(xActual, y, inicio.z);

        ActualizarVisual();
        ActualizarContacto();
    }

    // ---------------- Estados ----------------

    void CambiarA(Estado nuevo)
    {
        estado = nuevo;
        if (mostrarMensajes) Debug.Log(name + " -> " + nuevo);
        if (nuevo == Estado.Alerta)
        {
            temporizador = tiempoAlerta;
            AudioJuego.Sonar("alerta");                       // SONIDO
        }
        if (nuevo == Estado.Busca) temporizador = tiempoBuscar;
        if (iconoAlerta != null) iconoAlerta.SetActive(nuevo == Estado.Alerta || nuevo == Estado.Persigue);
    }

    void Patrullar()
    {
        if (temporizador > 0f) { temporizador -= Time.deltaTime; return; }   // esperando en el borde

        float izquierda = inicio.x - distanciaPatrulla;
        float derecha = inicio.x + distanciaPatrulla;

        // Si quedó fuera de su zona (por perseguir), primero vuelve
        if (xActual < izquierda) direccion = 1;
        else if (xActual > derecha) direccion = -1;

        xActual += direccion * velocidadPatrulla * Time.deltaTime;

        if (direccion == 1 && xActual >= derecha)
        { xActual = derecha; direccion = -1; temporizador = esperaEnBorde; }
        else if (direccion == -1 && xActual <= izquierda)
        { xActual = izquierda; direccion = 1; temporizador = esperaEnBorde; }
    }

    void Perseguir()
    {
        float lado = Mathf.Sign(alex.position.x - xActual);
        if (Mathf.Abs(alex.position.x - xActual) > 0.1f) direccion = (int)lado;

        float limiteIzq = inicio.x - distanciaPatrulla - margenPersecucion;
        float limiteDer = inicio.x + distanciaPatrulla + margenPersecucion;

        xActual = Mathf.MoveTowards(xActual, alex.position.x, velocidadPersecucion * Time.deltaTime);
        xActual = Mathf.Clamp(xActual, limiteIzq, limiteDer);
    }

    // ---------------- Visión ----------------

    bool PuedeVerAlex()
    {
        if (alex == null) return false;
        if (estadoAlex != null && estadoAlex.escondido) return false;   // ¡HIDE funciona!

        // Rectángulo de visión delante de ella
        Vector2 centro = (Vector2)transform.position + new Vector2(direccion * distanciaVision * 0.5f, 0f);
        Rect vision = new Rect(centro.x - distanciaVision * 0.5f, centro.y - altoVision * 0.5f, distanciaVision, altoVision);

        // ¿Alguna parte del cuerpo de Alex toca ese rectángulo?
        bool dentro;
        if (colAlex != null)
        {
            Bounds b = colAlex.bounds;
            Rect cuerpo = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            dentro = vision.Overlaps(cuerpo);
        }
        else dentro = vision.Contains(alex.position);
        if (!dentro) return false;

        // ¿Hay una pared en medio?
        if (capaParedes.value != 0)
        {
            RaycastHit2D golpe = Physics2D.Linecast(transform.position, colAlex != null ? (Vector2)colAlex.bounds.center : (Vector2)alex.position, capaParedes);
            if (golpe.collider != null) return false;
        }
        return true;
    }

    // ---------------- Visual y contacto ----------------

    void ActualizarVisual()
    {
        bool mirandoIzq = direccion < 0;
        if (sr != null) sr.flipX = spriteMiraIzquierda ? !mirandoIzq : mirandoIzq;

        if (conoVision != null)
        {
            // El cono se coloca delante de ella y gira con ella
            Vector3 p = conoVision.transform.localPosition;
            p.x = Mathf.Abs(p.x) * direccion;
            conoVision.transform.localPosition = p;
            conoVision.flipX = mirandoIzq;

            bool alerta = estado == Estado.Alerta || estado == Estado.Persigue;
            conoVision.color = Color.Lerp(conoVision.color, alerta ? colorConoAlerta : colorConoCalma, Time.deltaTime * 8f);
        }

        if (sr != null)
        {
            Color objetivo = estado == Estado.Persigue ? colorPersiguiendo : colorNormal;
            sr.color = Color.Lerp(sr.color, objetivo, Time.deltaTime * 6f);
        }
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

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoAlex>() != null) tocandoAlex = true;
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoAlex>() != null) tocandoAlex = false;
    }

    // ---------------- Ayudas en la Scene ----------------

    void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? inicio : transform.position;
        int dir = Application.isPlaying ? direccion : 1;

        // Celeste: zona de patrulla
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(c + Vector3.left * distanciaPatrulla, c + Vector3.right * distanciaPatrulla);

        // Rojo: hasta dónde puede perseguir
        Gizmos.color = Color.red;
        float m = distanciaPatrulla + margenPersecucion;
        Gizmos.DrawLine(c + new Vector3(-m, -0.3f), c + new Vector3(m, -0.3f));

        // Amarillo: rectángulo de visión
        Gizmos.color = Color.yellow;
        Vector3 centro = transform.position + new Vector3(dir * distanciaVision * 0.5f, 0f);
        Gizmos.DrawWireCube(centro, new Vector3(distanciaVision, altoVision, 0f));
    }
}