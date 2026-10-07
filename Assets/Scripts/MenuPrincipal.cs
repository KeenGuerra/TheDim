using UnityEngine;
using UnityEngine.SceneManagement;

// Menú principal: botón JUGAR y botón SALIR.
// Va en un objeto vacío llamado "MenuPrincipal" en la escena del menú.
// También funciona con teclado: Enter = Jugar, Esc = Salir.
public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la primera escena del juego (como aparece en la carpeta Scenes)")]
    public string primeraEscena = "Nivel 1";

    [Tooltip("Al empezar una partida nueva se borran las palabras aprendidas")]
    public bool partidaNueva = true;

    [Tooltip("El botón Salir (se esconde solo en la versión Web, porque en el navegador no se puede cerrar el juego)")]
    public GameObject botonSalir;

    void Start()
    {
        Time.timeScale = 1f;

        if (Application.platform == RuntimePlatform.WebGLPlayer && botonSalir != null)
            botonSalir.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Jugar();
        if (Input.GetKeyDown(KeyCode.Escape)) Salir();
    }

    // Conectar al botón JUGAR (On Click)
    public void Jugar()
    {
        if (partidaNueva)
        {
            PlayerPrefs.DeleteKey("PalabrasAprendidas");
            PlayerPrefs.Save();
        }
        SceneManager.LoadScene(primeraEscena);
    }

    // Conectar al botón SALIR (On Click)
    public void Salir()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // en el editor detiene el Play
#else
        Application.Quit();
#endif
    }
}