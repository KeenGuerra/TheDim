using System.Collections;
using UnityEngine;

// Zona invisible bajo el río o un vacío: si Alex cae, se corta la conexión
// y reaparece en el último farol. Va en un objeto con BoxCollider2D (Is Trigger).
public class ZonaCaida : MonoBehaviour
{
    [Tooltip("Partículas de salpicadura (opcional)")]
    public ParticleSystem salpicadura;

    private bool procesando = false;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (procesando) return;
        MovimientoAlex mov = otro.GetComponent<MovimientoAlex>();
        if (mov == null) return;

        StartCoroutine(Caida(mov));
    }

    IEnumerator Caida(MovimientoAlex mov)
    {
        procesando = true;
        Rigidbody2D rb = mov.GetComponent<Rigidbody2D>();
        Vector3 dondeCayo = mov.transform.position;

        if (salpicadura != null)
        {
            salpicadura.transform.position = new Vector3(dondeCayo.x, salpicadura.transform.position.y, 0f);
            salpicadura.Play();
        }
        AudioJuego.Sonar("caida");

        // Detiene a Alex para que no siga cayendo
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Se corta la conexión: el CorruptionManager lo lleva al último farol
        if (CorruptionManager.instancia != null)
            CorruptionManager.instancia.SubirCorrupcion(100f);

        // Espera a que lo muevan al farol (o máximo 6 segundos)
        float t = 0f;
        while (t < 6f && Vector3.Distance(mov.transform.position, dondeCayo) < 2f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        procesando = false;
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.35f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
