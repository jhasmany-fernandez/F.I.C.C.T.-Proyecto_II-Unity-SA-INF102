using UnityEngine;

// Se coloca sobre la meta para detectar cuando el jugador intenta completar el nivel.
public class GoalTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Solo el jugador puede activar la comprobacion de victoria.
        if (other.CompareTag("Player"))
        {
            JuegoManager.Instance?.IntentarCompletarNivel();
        }
    }
}
