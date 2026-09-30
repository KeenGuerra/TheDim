using UnityEngine;
using UnityEngine.UI;

public class IndicadorLight : MonoBehaviour
{
    [Tooltip("La palabra que muestra este indicador")]
    public string palabra = "LIGHT";

    [Header("Referencias (hijos)")]
    [Tooltip("Bombilla de fondo, se ve apagada (gris)")]
    public Image fondo;
    [Tooltip("Bombilla a color, tipo Filled: se llena mientras se recarga")]
    public Image carga;
    [Tooltip("Canvas Group del indicador, para que aparezca con un fundido")]
    public CanvasGroup grupo;

    [Header("Colores")]
    public Color colorApagado = new Color(0.35f, 0.33f, 0.4f, 1f);
    public Color colorRecargando = new Color(1f, 1f, 1f, 0.6f);

    private bool visible = false;
    private bool estabaEnCooldown = false;
    private float tiempoCooldown = 0f;
    private float salto = 0f;          // pequeño "pop" al aparecer o al recargarse
    private Vector3 escalaBase;

    void Start()
    {
        escalaBase = transform.localScale;
        if (grupo != null) grupo.alpha = 0f;   // oculto hasta aprender la palabra
        if (fondo != null) fondo.color = colorApagado;
        if (carga != null) carga.fillAmount = 1f;
    }

    void Update()
    {
        if (WordManager.instancia == null || CorruptionManager.instancia == null) return;

        // Aparece recién cuando el jugador aprende la palabra
        if (!visible && WordManager.instancia.PalabraAprendida(palabra))
        {
            visible = true;
            salto = 1f;
        }

        if (grupo != null)
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, visible ? 1f : 0f, Time.deltaTime * 3f);

        bool enCooldown = CorruptionManager.instancia.enCooldownLight;

        // Empezó el cooldown: la bombilla se vacía
        if (enCooldown && !estabaEnCooldown) tiempoCooldown = 0f;

        // Terminó el cooldown: pequeño salto para avisar que está lista
        if (!enCooldown && estabaEnCooldown) salto = 1f;

        estabaEnCooldown = enCooldown;

        float pulso = 0f;

        if (enCooldown)
        {
            // Se va llenando de abajo hacia arriba mientras se recarga
            tiempoCooldown += Time.deltaTime;
            float duracion = Mathf.Max(0.01f, CorruptionManager.instancia.duracionCooldownLight);
            if (carga != null)
            {
                carga.fillAmount = Mathf.Clamp01(tiempoCooldown / duracion);
                carga.color = colorRecargando;
            }
        }
        else
        {
            // Lista para usar: llena, a color y latiendo suave
            if (carga != null)
            {
                carga.fillAmount = 1f;
                carga.color = Color.white;
            }
            pulso = Mathf.Sin(Time.time * 3f) * 0.04f;
        }

        salto = Mathf.MoveTowards(salto, 0f, Time.deltaTime * 4f);
        transform.localScale = escalaBase * (1f + pulso + salto * 0.3f);
    }
}