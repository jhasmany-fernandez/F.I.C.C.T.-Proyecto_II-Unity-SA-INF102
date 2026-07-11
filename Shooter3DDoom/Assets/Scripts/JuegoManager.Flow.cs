using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Agrupa el flujo de partida: derrota, victoria, pausa, reintento y salida.
public partial class JuegoManager
{
    void ReintentarNivel()
    {
        // Reinicia la escena actual y restaura el time scale por si estaba pausado.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void JugadorMurio()
    {
        try
        {
            // Activa la pantalla de derrota cuando la vida del jugador llega a cero.
            if (estado != EstadoJuego.Jugando)
            {
                return;
            }

            estado = EstadoJuego.GameOver;
            Time.timeScale = 0f;
            CambiarCursor(true);
            MostrarPanel("Game Over", "Reintentar");
            MostrarEstado("Has caido", 0f);

            if (botonSecundario != null)
            {
                botonSecundario.gameObject.SetActive(true);
                botonSecundario.onClick.RemoveAllListeners();
                botonSecundario.onClick.AddListener(SalirDelJuego);

                Text textoSecundario = botonSecundario.GetComponentInChildren<Text>();
                if (textoSecundario != null)
                {
                    textoSecundario.text = "Salir";
                }
            }
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(JugadorMurio), ex);
        }
    }

    public void IntentarCompletarNivel()
    {
        // La meta consulta este metodo para validar si puede pasar de nivel o terminar el juego.
        if (estado != EstadoJuego.Jugando)
        {
            return;
        }

        // La meta solo funciona cuando ya no quedan enemigos vivos.
        if (enemigos.Count > 0)
        {
            MostrarEstado("La meta sigue cerrada: elimina a todos", 2f);
            return;
        }

        if (indiceNivelActual < niveles.Length - 1)
        {
            indiceNivelActual++;
            enemigosEliminadosAcumulados = 0;
            PrepararNivelActual(false);
            return;
        }

        estado = EstadoJuego.Victoria;
        Time.timeScale = 0f;
        CambiarCursor(true);
        MostrarPanel("Victoria", "Jugar otra vez");
        MostrarEstado("Nivel 3 completado", 0f);

        if (botonSecundario != null)
        {
            botonSecundario.gameObject.SetActive(true);
            botonSecundario.onClick.RemoveAllListeners();
            botonSecundario.onClick.AddListener(SalirDelJuego);

            Text textoSecundario = botonSecundario.GetComponentInChildren<Text>();
            if (textoSecundario != null)
            {
                textoSecundario.text = "Salir";
            }
        }
    }

    void MostrarConfirmacionSalida()
    {
        // Reutiliza el panel final como confirmacion antes de salir.
        estado = EstadoJuego.Pausado;
        salirJuegoPendiente = true;
        pausaActiva = false;
        Time.timeScale = 0f;
        CambiarCursor(true);
        MostrarPanel("Deseas salir del juego?", "Salir");
        MostrarEstado("Pulsa Esc para continuar", 0f);

        botonReintentar.onClick.RemoveAllListeners();
        botonReintentar.onClick.AddListener(SalirDelJuego);

        Text textoPrincipal = botonReintentar.GetComponentInChildren<Text>();
        if (textoPrincipal != null)
        {
            textoPrincipal.text = "Salir";
        }

        if (botonSecundario != null)
        {
            botonSecundario.gameObject.SetActive(true);
            botonSecundario.onClick.RemoveAllListeners();
            botonSecundario.onClick.AddListener(ReanudarJuego);

            Text textoSecundario = botonSecundario.GetComponentInChildren<Text>();
            if (textoSecundario != null)
            {
                textoSecundario.text = "Cancelar";
            }
        }
    }

    void ReanudarJuego()
    {
        // Cierra cualquier panel de pausa o salida y devuelve el control al jugador.
        if (!salirJuegoPendiente && !pausaActiva)
        {
            return;
        }

        salirJuegoPendiente = false;
        pausaActiva = false;
        estado = EstadoJuego.Jugando;
        Time.timeScale = 1f;
        CambiarCursor(false);

        if (panelFinal != null)
        {
            panelFinal.SetActive(false);
        }

        MostrarEstado("Elimina a todos y llega a la meta", 0f);

        botonReintentar.onClick.RemoveAllListeners();
        botonReintentar.onClick.AddListener(ReintentarNivel);
    }

    void MostrarPausa()
    {
        // Pausa el juego y transforma el panel en menu temporal.
        estado = EstadoJuego.Pausado;
        pausaActiva = true;
        salirJuegoPendiente = false;
        Time.timeScale = 0f;
        CambiarCursor(true);
        MostrarPanel("Juego en pausa", "Continuar");
        MostrarEstado("Presiona Esc para reanudar", 0f);

        botonReintentar.onClick.RemoveAllListeners();
        botonReintentar.onClick.AddListener(ReanudarJuego);

        Text textoPrincipal = botonReintentar.GetComponentInChildren<Text>();
        if (textoPrincipal != null)
        {
            textoPrincipal.text = "Continuar";
        }

        if (botonSecundario != null)
        {
            botonSecundario.gameObject.SetActive(true);
            botonSecundario.onClick.RemoveAllListeners();
            botonSecundario.onClick.AddListener(MostrarConfirmacionSalida);

            Text textoSecundario = botonSecundario.GetComponentInChildren<Text>();
            if (textoSecundario != null)
            {
                textoSecundario.text = "Salir";
            }
        }
    }

    void SalirDelJuego()
    {
        // En editor detiene el Play Mode; en build cierra la aplicacion.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
