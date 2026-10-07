using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menú de pausa: presiona ESC (o P) mientras juegas.
//   CONTINUAR      -> sigue jugando (o vuelve a presionar ESC)
//   SALIR AL MENÚ  -> vuelve a la escena del menú principal
// Va en el objeto "MenuPausa" dentro del Canvas de cada nivel.
public class MenuPausa : MonoBehaviour
{
    [Tooltip("El panel oscuro con los botones (hijo de este objeto). Empieza apagado.")]
    public GameObject panel;

    [Tooltip("Nombre EXACTO de la escena del menú principal")]
    public string escenaMenu = "Menu";

    public KeyCode tecla = KeyCode.Escape;
    [Tooltip("Segunda tecla (útil en la versión Web, donde ESC sale de pantalla completa)")]
    public KeyCode teclaExtra = KeyCode.P;

    public bool pausado { get; private set; }

    // Scripts de Alex que se apagan durante la pausa (para que no salte ni dispare al tocar teclas)
    private readonly List<MonoBehaviour> apagados = new List<MonoBehaviour>();

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(tecla) || Input.GetKeyDown(teclaExtra))
        {
            if (pausado) Continuar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        pausado = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        ApagarControlesAlex();
    }

    // Conectar al botón CONTINUAR (On Click)
    public void Continuar()
    {
        pausado = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        EncenderControlesAlex();
    }

    // Conectar al botón SALIR AL MENÚ (On Click)
    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(escenaMenu);
    }

    void ApagarControlesAlex()
    {
        apagados.Clear();
        MovimientoAlex mov = FindFirstObjectByType<MovimientoAlex>();
        if (mov == null) return;

        foreach (MonoBehaviour m in mov.GetComponentsInChildren<MonoBehaviour>())
        {
            if (m != null && m.enabled && m != this)
            {
                m.enabled = false;
                apagados.Add(m);
            }
        }
    }

    void EncenderControlesAlex()
    {
        foreach (MonoBehaviour m in apagados) if (m != null) m.enabled = true;
        apagados.Clear();
    }
}
