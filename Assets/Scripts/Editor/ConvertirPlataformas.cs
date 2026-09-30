using UnityEditor;
using UnityEngine;

public class ConvertirPlataformas : EditorWindow
{
    private Sprite sprite;
    private string sortingLayer = "Jugables";
    [Range(0.1f, 1f)] private float fraccionTabla = 0.6f; // parte de arriba que es sólida (sin musgo colgante)
    private bool agregarEffector = true;

    [MenuItem("Tools/Convertir plataformas")]
    static void Abrir()
    {
        GetWindow<ConvertirPlataformas>("Plataformas");
    }

    void OnGUI()
    {
        sprite = (Sprite)EditorGUILayout.ObjectField("Sprite nuevo", sprite, typeof(Sprite), false);
        sortingLayer = EditorGUILayout.TextField("Sorting Layer", sortingLayer);
        fraccionTabla = EditorGUILayout.Slider("Alto sólido (fracción)", fraccionTabla, 0.1f, 1f);
        agregarEffector = EditorGUILayout.Toggle("Atravesable desde abajo", agregarEffector);

        EditorGUILayout.HelpBox(
            "1. Selecciona las plataformas en la Hierarchy.\n" +
            "2. Pulsa el botón. Se puede deshacer con Ctrl + Z.",
            MessageType.Info);

        GUI.enabled = sprite != null && Selection.gameObjects.Length > 0;
        if (GUILayout.Button("Convertir " + Selection.gameObjects.Length + " seleccionadas"))
        {
            Convertir();
        }
        GUI.enabled = true;
    }

    void OnSelectionChange()
    {
        Repaint();
    }

    void Convertir()
    {
        float ppu = sprite.pixelsPerUnit;
        float alto = sprite.rect.height / ppu;
        float puntas = (sprite.border.x + sprite.border.z) / ppu;
        float medio = sprite.rect.width / ppu - puntas;
        float anchoMinimo = puntas + medio * 0.3f; // para que las puntas nunca se aplasten
        int convertidas = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            // Medidas actuales en el mundo
            float ancho = Mathf.Max(sr.bounds.size.x, anchoMinimo);
            float bordeSuperior = sr.bounds.max.y;
            float centroX = sr.bounds.center.x;

            Undo.RecordObject(go.transform, "Convertir plataforma");
            Undo.RecordObject(sr, "Convertir plataforma");

            go.transform.localScale = new Vector3(1f, 1f, go.transform.localScale.z);

            sr.sprite = sprite;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive;   // repite tramos completos, sin cortarlos
            sr.adaptiveModeThreshold = 0.5f;
            sr.size = new Vector2(ancho, alto);
            sr.sortingLayerName = sortingLayer;

            // La superficie donde se pisa queda a la misma altura que antes
            go.transform.position = new Vector3(centroX, bordeSuperior - alto / 2f, go.transform.position.z);

            // Collider: solo la tabla (la parte de arriba), sin el musgo que cuelga
            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
            Undo.RecordObject(col, "Convertir plataforma");
            float altoSolido = alto * fraccionTabla;
            col.isTrigger = false;
            col.autoTiling = false;
            col.size = new Vector2(ancho, altoSolido);
            col.offset = new Vector2(0f, alto / 2f - altoSolido / 2f);

            if (agregarEffector)
            {
                if (go.GetComponent<PlatformEffector2D>() == null)
                    Undo.AddComponent<PlatformEffector2D>(go);
                col.usedByEffector = true;
            }

            EditorUtility.SetDirty(go);
            convertidas++;
        }

        Debug.Log("Plataformas convertidas: " + convertidas);
    }
}