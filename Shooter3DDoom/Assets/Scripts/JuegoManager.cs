using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Archivo base del gestor principal. Contiene el ciclo de vida comun y el estado global del juego.
public partial class JuegoManager : MonoBehaviour
{
    // Estados generales del juego para habilitar o bloquear controles y UI.
    enum EstadoJuego
    {
        Jugando,
        Pausado,
        GameOver,
        Victoria
    }

    // Datos minimos para describir cada nivel.
    struct DatosNivel
    {
        public int cantidadEnemigos;
        public Color colorEnemigo;
        public string nombre;
    }

    // Singleton para que el resto de scripts consulte el estado del juego sin buscar referencias.
    public static JuegoManager Instance { get; private set; }

    // Lista viva de enemigos actuales del nivel.
    private readonly List<EnemyAI> enemigos = new();
    // Lista viva de botiquines actuales del nivel.
    private readonly List<GameObject> botiquines = new();
    // Configuracion basica de los tres niveles del juego.
    private readonly DatosNivel[] niveles =
    {
        new DatosNivel { cantidadEnemigos = 4, colorEnemigo = new Color(0.78f, 0.18f, 0.18f, 1f), nombre = "Nivel 1" },
        new DatosNivel { cantidadEnemigos = 5, colorEnemigo = new Color(0.9f, 0.78f, 0.15f, 1f), nombre = "Nivel 2" },
        new DatosNivel { cantidadEnemigos = 6, colorEnemigo = new Color(0.2f, 0.78f, 0.25f, 1f), nombre = "Nivel 3" },
    };

    // Estado actual de la partida.
    private EstadoJuego estado = EstadoJuego.Jugando;
    // Referencias a UI creadas o recuperadas en runtime.
    private Canvas canvas;
    private Text textoMunicion;
    private Text textoEnemigos;
    private Text textoEstado;
    private Text textoVida;
    private Text textoMira;
    private Text textoArma;
    private Text textoCuentaRegresiva;
    private Image overlayDano;
    private GameObject panelFinal;
    private Text textoPanel;
    private Button botonReintentar;
    private Button botonSecundario;
    // Referencias del jugador y de su disparo para enlazar enemigos y HUD.
    private Vida vidaJugador;
    private Transform jugador;
    private AudioClip clipDisparoJugador;
    // Referencias visuales del arma en pantalla.
    private RectTransform armaRect;
    private Image armaImagen;
    private Vector2 armaPosicionBase;
    private Vector2 armaTamanoBase;
    // Rutinas activas para animaciones UI y flashes.
    private Coroutine rutinaDano;
    private Coroutine rutinaEstado;
    private Coroutine rutinaArma;
    private Coroutine rutinaCuentaRegresiva;
    private Coroutine rutinaCuracion;
    // Banderas auxiliares para pausa y confirmacion de salida.
    private bool salirJuegoPendiente;
    private bool pausaActiva;
    // Variables del flujo de nivel y del sistema de caceria.
    private float tiempoInicioNivel;
    private int enemigosEliminadosAcumulados;
    private int indiceNivelActual;
    private GameObject metaActual;
    private List<Vector3> puntosNivelDisponibles;
    private int ultimoIndiceMeta = -1;
    // Tiempo de ventaja inicial para que el jugador ataque antes de que reaccionen los enemigos.
    private const float TiempoCaceriaEnemiga = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CrearInstancia()
    {
        // Mantiene un unico gestor global aunque la escena se recargue.
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("JuegoManager");
        DontDestroyOnLoad(go);
        go.AddComponent<JuegoManager>();
    }

    void Awake()
    {
        // Si ya existia otro gestor, este se destruye para respetar el singleton.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        // Escucha la carga de escena para reconstruir referencias y UI.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Limpia la suscripcion al desactivar el objeto.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar escena reinicia efectos visuales y reconfigura todo de forma diferida.
        DetenerRutinasVisuales();
        LimpiarReferenciasUI();
        StartCoroutine(ConfigurarEscena());
    }

    IEnumerator ConfigurarEscena()
    {
        yield return null;

        // Reinicia el estado del juego y vuelve a enlazar referencias de la escena.
        estado = EstadoJuego.Jugando;
        enemigos.Clear();
        botiquines.Clear();
        Time.timeScale = 1f;
        tiempoInicioNivel = Time.time;
        enemigosEliminadosAcumulados = 0;
        indiceNivelActual = 0;

        jugador = GameObject.FindWithTag("Player")?.transform;
        vidaJugador = jugador != null ? jugador.GetComponent<Vida>() : null;

        Disparar dispararJugador = jugador != null ? jugador.GetComponent<Disparar>() : null;
        clipDisparoJugador = dispararJugador != null ? dispararJugador.sonidoDisparo : null;

        ConfigurarNavMesh();
        ConfigurarUI();
        PrepararNivelActual(true);

        if (vidaJugador != null)
        {
            ActualizarVida(vidaJugador.VidaActual, vidaJugador.VidaMaxima);
        }

        if (dispararJugador != null)
        {
            ActualizarMunicion(dispararJugador.BalasActuales, dispararJugador.BalasPorCargador, dispararJugador.EstaRecargando);
        }

        MostrarEstado("Tienes 5 segundos para cazarlos primero");
        IniciarCuentaRegresiva();
        CambiarCursor(false);
        salirJuegoPendiente = false;
        pausaActiva = false;
    }

    void Update()
    {
        // Escape alterna entre pausa y reanudacion si el estado actual lo permite.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estado == EstadoJuego.Jugando)
            {
                MostrarPausa();
            }
            else if (estado == EstadoJuego.Pausado && pausaActiva)
            {
                ReanudarJuego();
            }
        }
    }

    // Propiedad consultada por scripts del jugador para bloquear movimiento y disparo.
    public bool PuedeControlarJugador => estado == EstadoJuego.Jugando;

    // Propiedad consultada por la IA para decidir si debe moverse.
    public bool PuedeActuarIA => estado == EstadoJuego.Jugando;

    // Despues del tiempo inicial, los enemigos dejan de esperar y comienzan la caceria.
    public bool EnemigosPuedenCazarJugador => estado == EstadoJuego.Jugando && Time.time >= tiempoInicioNivel + TiempoCaceriaEnemiga;

    // Tiempo restante mostrado en pantalla para la cuenta regresiva inicial.
    public float TiempoRestanteCaceria => Mathf.Max(0f, (tiempoInicioNivel + TiempoCaceriaEnemiga) - Time.time);
}
