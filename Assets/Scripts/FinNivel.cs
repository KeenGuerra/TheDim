using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FinNivel : MonoBehaviour
{
    [Header("Pantalla de transición")]
    public CanvasGroup pantalla;
    public TextMeshProUGUI frase;
    public TextMeshProUGUI continuar;
    [TextArea] public string textoFrase = "La bicicleta de Sam... Él estuvo aquí.";
    public float velocidadEscritura = 0.05f;
    public float duracionFundido = 1.2f;

    [Header("Siguiente nivel")]
    [Tooltip("Nombre exacto de la escena del Nivel 2. Si está vacío, se reinicia este nivel.")]
    public string escenaSiguiente = "";
    public int numeroNivel = 1;

    private bool terminado = false;
    private bool puedeContinuar = false;

    void Start()
    {
        if (pantalla != null)
        {
            pantalla.alpha = 0f;
            pantalla.blocksRaycasts = false;
        }
        if (frase != null) frase.text = "";
        if (continuar != null) continuar.alpha = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (terminado || !other.CompareTag("Player")) return;
        StartCoroutine(Terminar(other.gameObject));
    }

    IEnumerator Terminar(GameObject alex)
    {
        terminado = true;

        // Alex se detiene
        MovimientoAlex movimiento = alex.GetComponent<MovimientoAlex>();
        if (movimiento != null) movimiento.enabled = false;

        Rigidbody2D rb = alex.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Animator anim = alex.GetComponent<Animator>();
        if (anim != null) anim.Play("Alex_Idle");

        // La corrupción deja de subir
        if (CorruptionManager.instancia != null) CorruptionManager.instancia.enabled = false;

        Guardar();

        // Fundido hacia la pantalla de transición
        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.deltaTime;
            if (pantalla != null) pantalla.alpha = Mathf.Clamp01(t / duracionFundido);
            yield return null;
        }

        // La frase se escribe letra por letra
        if (frase != null)
        {
            frase.text = "";
            foreach (char c in textoFrase)
            {
                frase.text += c;
                yield return new WaitForSeconds(velocidadEscritura);
            }
        }

        yield return new WaitForSeconds(0.8f);
        puedeContinuar = true;
    }

    void Update()
    {
        if (!puedeContinuar) return;

        // "Presiona E para continuar" titila suave
        if (continuar != null)
            continuar.alpha = 0.5f + Mathf.Sin(Time.time * 3f) * 0.5f;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            puedeContinuar = false;

            if (!string.IsNullOrEmpty(escenaSiguiente))
                SceneManager.LoadScene(escenaSiguiente);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);   // reinicia (demo)
        }
    }

    void Guardar()
    {
        int completadoAntes = PlayerPrefs.GetInt("NivelCompletado", 0);
        PlayerPrefs.SetInt("NivelCompletado", Mathf.Max(completadoAntes, numeroNivel));

        if (WordManager.instancia != null)
            PlayerPrefs.SetString("PalabrasAprendidas",
                string.Join(",", WordManager.instancia.palabrasAprendidas));

        PlayerPrefs.Save();
        Debug.Log("Progreso guardado: nivel " + numeroNivel + " completado.");
    }
}