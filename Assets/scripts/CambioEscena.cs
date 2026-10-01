using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    public string nombreEscena = "lvl2";   // Se puede cambiar desde el Inspector

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<player>() != null)
        {
            SceneManager.LoadScene(nombreEscena);
        }
    }
}