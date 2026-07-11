using UnityEngine;

// Se coloca sobre la meta para detectar cuando el jugador intenta completar el nivel.
public class GoalTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        try
        {
            // Solo el jugador puede activar la comprobacion de victoria.
            if (other.CompareTag("Player"))
            {
                JuegoManager.Instance?.IntentarCompletarNivel();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GoalTrigger] Error en OnTriggerEnter: {ex.Message}\n{ex}", this);
        }
    }
}
