using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // Opcional: un objeto hijo que marca el punto exacto de reaparición.
    // Si lo dejas vacío, reaparece en la posición de este mismo objeto.
    public Transform respawnPoint;

    void OnTriggerEnter(Collider other)
    {
        player p = other.GetComponentInParent<player>();
        if (p != null)
        {
            Vector3 destino = respawnPoint != null ? respawnPoint.position : transform.position;
            p.SetCheckpoint(destino);
        }
    }
}