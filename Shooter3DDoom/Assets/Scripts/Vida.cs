using UnityEngine;

public class Vida : MonoBehaviour
{
    public int vidaMax = 3;
    public bool esJugador = false;

    private int vidaActual;
    private bool estaMuerto;
    private Renderer cachedRenderer;
    private Color colorOriginal;
    private Coroutine rutinaImpacto;

    public System.Action<Vida> alMorir;

    public int VidaActual => vidaActual;

    public int VidaMaxima => vidaMax;

    public bool EstaMuerto => estaMuerto;

    void Awake()
    {
        vidaActual = vidaMax;
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_BaseColor"))
        {
            colorOriginal = cachedRenderer.material.GetColor("_BaseColor");
        }
        else if (cachedRenderer != null)
        {
            colorOriginal = cachedRenderer.material.color;
        }
    }

    public void RecibirDano(int cantidad)
    {
        if (estaMuerto || cantidad <= 0)
        {
            return;
        }

        vidaActual = Mathf.Max(vidaActual - cantidad, 0);

        if (esJugador)
        {
            JuegoManager.Instance?.ActualizarVida(vidaActual, vidaMax);
            JuegoManager.Instance?.MostrarFlashDano();
        }
        else
        {
            MostrarImpacto();
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public int Curar(int cantidad)
    {
        if (estaMuerto || cantidad <= 0)
        {
            return 0;
        }

        int vidaAntes = vidaActual;
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMax);

        if (esJugador)
        {
            JuegoManager.Instance?.ActualizarVida(vidaActual, vidaMax);
        }

        return vidaActual - vidaAntes;
    }

    void Morir()
    {
        if (esJugador)
        {
            estaMuerto = true;
            alMorir?.Invoke(this);
            JuegoManager.Instance?.JugadorMurio();
            return;
        }

        estaMuerto = true;
        alMorir?.Invoke(this);
        Destroy(gameObject);
    }

    void MostrarImpacto()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        if (rutinaImpacto != null)
        {
            StopCoroutine(rutinaImpacto);
        }

        rutinaImpacto = StartCoroutine(ImpactoRutina());
    }

    System.Collections.IEnumerator ImpactoRutina()
    {
        AplicarColorRenderer(Color.yellow);
        yield return new WaitForSeconds(0.12f);
        AplicarColorRenderer(colorOriginal);
        rutinaImpacto = null;
    }

    void AplicarColorRenderer(Color color)
    {
        if (cachedRenderer == null)
        {
            return;
        }

        if (cachedRenderer.material.HasProperty("_BaseColor"))
        {
            cachedRenderer.material.SetColor("_BaseColor", color);
        }
        else
        {
            cachedRenderer.material.color = color;
        }
    }
}
