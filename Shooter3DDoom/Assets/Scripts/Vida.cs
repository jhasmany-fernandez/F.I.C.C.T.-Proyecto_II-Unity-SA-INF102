using UnityEngine;

// Gestiona vida, dano, curacion y muerte tanto para jugador como para enemigos.
public class Vida : MonoBehaviour
{
    // Vida maxima configurable desde el editor.
    public int vidaMax = 3;
    // Indica si este componente pertenece al jugador para cambiar el comportamiento visual.
    public bool esJugador = false;

    // Vida actual interna.
    private int vidaActual;
    // Evita procesar dano o muerte varias veces.
    private bool estaMuerto;
    // Renderer usado para mostrar el impacto en enemigos.
    private Renderer cachedRenderer;
    // Color base original del material para restaurarlo tras el impacto.
    private Color colorOriginal;
    // Rutina visual del impacto para no superponer varias a la vez.
    private Coroutine rutinaImpacto;

    // Evento opcional para notificar cuando el objeto muere.
    public System.Action<Vida> alMorir;

    void RegistrarError(string contexto, System.Exception ex)
    {
        Debug.LogError($"[Vida] Error en {contexto}: {ex.Message}\n{ex}", this);
    }

    // Propiedad de solo lectura para consultar la vida actual.
    public int VidaActual => vidaActual;

    // Propiedad de solo lectura para consultar la vida maxima.
    public int VidaMaxima => vidaMax;

    // Propiedad de solo lectura para saber si ya murio.
    public bool EstaMuerto => estaMuerto;

    void Awake()
    {
        try
        {
            // Inicializa la vida actual con el maximo definido.
            vidaActual = vidaMax;
            // Intenta cachear un renderer hijo para efectos visuales de impacto.
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
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Awake), ex);
        }
    }

    public void RecibirDano(int cantidad)
    {
        try
        {
            // Ignora dano invalido o repetido despues de morir.
            if (estaMuerto || cantidad <= 0)
            {
                return;
            }

            // Nunca deja la vida por debajo de cero.
            vidaActual = Mathf.Max(vidaActual - cantidad, 0);

            // El jugador actualiza HUD y flash rojo; los enemigos muestran impacto local.
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
        catch (System.Exception ex)
        {
            RegistrarError(nameof(RecibirDano), ex);
        }
    }

    public int Curar(int cantidad)
    {
        // No se puede curar a un objeto muerto ni con cantidades invalidas.
        if (estaMuerto || cantidad <= 0)
        {
            return 0;
        }

        // Calcula cuanto se recupero realmente respetando el tope maximo.
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
        // El jugador no se destruye: delega el flujo de derrota al JuegoManager.
        if (esJugador)
        {
            estaMuerto = true;
            alMorir?.Invoke(this);
            JuegoManager.Instance?.JugadorMurio();
            return;
        }

        // Los enemigos avisan su muerte y luego se destruyen.
        estaMuerto = true;
        alMorir?.Invoke(this);
        Destroy(gameObject);
    }

    void MostrarImpacto()
    {
        // Si no hay renderer, simplemente omite el efecto visual.
        if (cachedRenderer == null)
        {
            return;
        }

        // Reinicia la rutina si el enemigo recibe varios impactos seguidos.
        if (rutinaImpacto != null)
        {
            StopCoroutine(rutinaImpacto);
        }

        rutinaImpacto = StartCoroutine(ImpactoRutina());
    }

    System.Collections.IEnumerator ImpactoRutina()
    {
        // Tiñe al enemigo de amarillo un instante y luego restaura el color base.
        AplicarColorRenderer(Color.yellow);
        yield return new WaitForSeconds(0.12f);
        AplicarColorRenderer(colorOriginal);
        rutinaImpacto = null;
    }

    void AplicarColorRenderer(Color color)
    {
        // Soporta materiales URP y materiales legacy.
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
