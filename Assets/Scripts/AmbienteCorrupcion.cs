using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AmbienteCorrupcion : MonoBehaviour
{
    [Tooltip("La Global Light 2D de la escena")]
    public Light2D luzGlobal;

    [Header("Corrupción baja (pueblo cálido)")]
    public Color colorCalido = new Color(1f, 0.93f, 0.85f);
    public float intensidadBaja = 0.6f;

    [Header("Corrupción alta (pueblo enfermo)")]
    public Color colorEnfermo = new Color(0.55f, 0.7f, 0.45f);
    public float intensidadAlta = 0.3f;

    [Header("Ajustes")]
    [Tooltip("Desde qué nivel de corrupción empieza a notarse el cambio")]
    [Range(0, 100)] public float empiezaEn = 30f;
    [Tooltip("Qué tan rápido se ajusta el color (más alto = más rápido)")]
    public float velocidadCambio = 1.5f;
    [Tooltip("Cuánto titila la luz cuando la corrupción está muy alta")]
    [Range(0f, 0.5f)] public float fuerzaTitileo = 0.2f;

    private float nivel = 0f;   // 0 = pueblo sano, 1 = pueblo corrompido

    void Start()
    {
        if (luzGlobal == null) luzGlobal = GetComponent<Light2D>();
    }

    void Update()
    {
        if (luzGlobal == null || CorruptionManager.instancia == null) return;

        // Pasa la corrupción (0–100) a un valor de 0 a 1, empezando en "empiezaEn"
        float objetivo = Mathf.InverseLerp(empiezaEn, 100f, CorruptionManager.instancia.corrupcionActual);

        // Cambio suave, sin saltos bruscos
        nivel = Mathf.Lerp(nivel, objetivo, Time.deltaTime * velocidadCambio);

        luzGlobal.color = Color.Lerp(colorCalido, colorEnfermo, nivel);
        float intensidad = Mathf.Lerp(intensidadBaja, intensidadAlta, nivel);

        // Por encima del 60 %, la luz empieza a fallar, como cables viejos
        if (nivel > 0.6f)
        {
            float fuerza = (nivel - 0.6f) / 0.4f * fuerzaTitileo;
            intensidad *= 1f - Mathf.PerlinNoise(Time.time * 8f, 0f) * fuerza;
        }

        luzGlobal.intensity = intensidad;
    }
}