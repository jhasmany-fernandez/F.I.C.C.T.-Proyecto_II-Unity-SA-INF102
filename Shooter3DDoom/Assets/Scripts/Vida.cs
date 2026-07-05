using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Vida : MonoBehaviour
{
    
    public int vidaMax = 3;
    public bool esJugador = false;
    private int vidaActual;

    void Start()
    {
        vidaActual = vidaMax;
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0) Morir();
    }

    void Morir()
    {
        if (esJugador)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // reinicia
        else
            Destroy(gameObject);
    }

    public int VidaActual()
    {
        return vidaActual;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
