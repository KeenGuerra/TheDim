using UnityEngine;
using UnityEngine.UI;

// Ícono de RUN en el HUD: aparece al aprender la palabra,
// se pone brillante mientras corre y se llena mientras se recarga.
public class IndicadorRun : MonoBehaviour
{
    public HabilidadRun habilidad;

    [Header("Referencias (hijos)")]
    public Image fondo;
    public Image carga;      // Image Type: Filled
    public CanvasGroup grupo;

    [Header("Colores")]
    public Color colorApagado = new Color(0.35f, 0.33f, 0.4f, 1f);
    public Color colorRecargando = new Color(1f, 1f, 1f, 0.6f);
    public Color colorActiva = new Color(1f, 0.9f, 0.6f, 1f);

    private bool visible = false;
    private bool estabaEnCooldown = false;
    private float salto = 0f;
    private Vector3 escalaBase;

    void Start()
    {
        escalaBase = transform.localScale;
        if (habilidad == null) habilidad = FindFirstObjectByType<HabilidadRun>();
        if (grupo != null) grupo.alpha = 0f;
        if (fondo != null) fondo.color = colorApagado;
        if (carga != null) carga.fillAmount = 1f;
    }

    void Update()
    {
        if (habilidad == null || WordManager.instancia == null) return;

        if (!visible && WordManager.instancia.PalabraAprendida(habilidad.palabra))
        {
            visible = true;
            salto = 1f;
        }
        if (grupo != null)
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, visible ? 1f : 0f, Time.deltaTime * 3f);

        // Terminó de recargar: pequeño salto
        if (!habilidad.enCooldown && estabaEnCooldown) salto = 1f;
        estabaEnCooldown = habilidad.enCooldown;

        float pulso = 0f;
        if (carga != null)
        {
            if (habilidad.activa)
            {
                carga.fillAmount = 1f;
                carga.color = colorActiva;
                pulso = Mathf.Sin(Time.time * 20f) * 0.06f;   // vibra mientras corre
            }
            else if (habilidad.enCooldown)
            {
                carga.fillAmount = habilidad.progresoCooldown;
                carga.color = colorRecargando;
            }
            else
            {
                carga.fillAmount = 1f;
                carga.color = Color.white;
                pulso = Mathf.Sin(Time.time * 3f) * 0.04f;
            }
        }

        salto = Mathf.MoveTowards(salto, 0f, Time.deltaTime * 4f);
        transform.localScale = escalaBase * (1f + pulso + salto * 0.3f);
    }
}
