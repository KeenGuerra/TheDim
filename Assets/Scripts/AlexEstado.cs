using UnityEngine;

public class AlexEstado : MonoBehaviour
{
    [Header("HIDE")]
    public bool escondido = false;
    public float duracionHide = 3f;
    [Range(0f, 1f)] public float transparenciaOculto = 0.4f;

    private float temporizador = 0f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Tecla para usar HIDE (solo si la palabra ya está aprendida)
        if (Input.GetKeyDown(KeyCode.H) && !escondido &&
            WordManager.instancia.PalabraAprendida("HIDE"))
        {
            ActivarHide();
        }

        if (escondido)
        {
            temporizador -= Time.deltaTime;
            if (temporizador <= 0f)
            {
                escondido = false;
                CambiarTransparencia(1f);
                Debug.Log("El efecto HIDE terminó.");
            }
        }
    }

    public void ActivarHide()
    {
        escondido = true;
        temporizador = duracionHide;
        CambiarTransparencia(transparenciaOculto);
        Debug.Log("HIDE activado.");
    }

    void CambiarTransparencia(float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}