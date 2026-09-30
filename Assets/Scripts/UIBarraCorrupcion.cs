using UnityEngine;
using UnityEngine.UI;

public class UIBarraCorrupcion : MonoBehaviour
{
    public Slider slider;

    void Update()
    {
        if (CorruptionManager.instancia != null)
        {
            slider.value = CorruptionManager.instancia.corrupcionActual;
        }
    }
}