using UnityEngine;
using TMPro;

// Letra que aparece en la ventana de calma de El Susurro.
// Va en el prefab "LetraJefe" (copia de tu prefab de Letra, SIN el script Letra).
// Necesita un Collider 2D con Is Trigger y un hijo con TextMeshPro.
public class LetraJefe : MonoBehaviour
{
    [HideInInspector] public ElSusurro jefe;

    public TextMeshPro texto;
    public float alturaFlote = 0.2f;
    public float velocidadFlote = 2.5f;

    private Vector3 inicio;
    private Vector3 escalaBase;
    private bool ayuda = false;
    private bool recogida = false;

    void Awake()
    {
        if (texto == null) texto = GetComponentInChildren<TextMeshPro>();
        escalaBase = transform.localScale;
    }

    void Start()
    {
        inicio = transform.position;
    }

    public void Configurar(char letra, bool conAyuda)
    {
        if (texto == null) texto = GetComponentInChildren<TextMeshPro>();
        if (texto != null) texto.text = letra.ToString();
        ayuda = conAyuda;
    }

    void Update()
    {
        if (recogida) return;

        float y = Mathf.Sin(Time.time * velocidadFlote) * alturaFlote;
        transform.position = inicio + Vector3.up * y;

        // Con ayuda: más grande y latiendo fuerte para que se note
        float pulso = ayuda ? 1.4f + 0.2f * Mathf.Sin(Time.time * 8f) : 1f + 0.06f * Mathf.Sin(Time.time * 3f);
        transform.localScale = escalaBase * pulso;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (recogida || otro.GetComponent<MovimientoAlex>() == null) return;
        recogida = true;
        if (jefe != null) jefe.LetraRecogida();
        Destroy(gameObject);
    }
}
