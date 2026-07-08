using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JuegoManager : MonoBehaviour
{
    enum EstadoJuego
    {
        Jugando,
        Pausado,
        GameOver,
        Victoria
    }

    struct DatosNivel
    {
        public int cantidadEnemigos;
        public Color colorEnemigo;
        public string nombre;
    }

    public static JuegoManager Instance { get; private set; }

    private readonly List<EnemyAI> enemigos = new();
    private readonly List<GameObject> botiquines = new();
    private readonly DatosNivel[] niveles =
    {
        new DatosNivel { cantidadEnemigos = 4, colorEnemigo = new Color(0.78f, 0.18f, 0.18f, 1f), nombre = "Nivel 1" },
        new DatosNivel { cantidadEnemigos = 5, colorEnemigo = new Color(0.9f, 0.78f, 0.15f, 1f), nombre = "Nivel 2" },
        new DatosNivel { cantidadEnemigos = 6, colorEnemigo = new Color(0.2f, 0.78f, 0.25f, 1f), nombre = "Nivel 3" },
    };

    private EstadoJuego estado = EstadoJuego.Jugando;
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
    private Vida vidaJugador;
    private Transform jugador;
    private AudioClip clipDisparoJugador;
    private RectTransform armaRect;
    private Image armaImagen;
    private Vector2 armaPosicionBase;
    private Vector2 armaTamanoBase;
    private Coroutine rutinaDano;
    private Coroutine rutinaEstado;
    private Coroutine rutinaArma;
    private Coroutine rutinaCuentaRegresiva;
    private Coroutine rutinaCuracion;
    private bool salirJuegoPendiente;
    private bool pausaActiva;
    private float tiempoInicioNivel;
    private int enemigosEliminadosAcumulados;
    private int indiceNivelActual;
    private GameObject metaActual;
    private List<Vector3> puntosNivelDisponibles;
    private int ultimoIndiceMeta = -1;
    private const float TiempoCaceriaEnemiga = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CrearInstancia()
    {
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetenerRutinasVisuales();
        LimpiarReferenciasUI();
        StartCoroutine(ConfigurarEscena());
    }

    IEnumerator ConfigurarEscena()
    {
        yield return null;

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

    void ConfigurarNavMesh()
    {
        GameObject piso = GameObject.Find("Piso");
        if (piso == null)
        {
            return;
        }

        NavMeshSurface surface = piso.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = piso.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    void ConfigurarUI()
    {
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        textoMunicion = CrearTextoUI("AmmoText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -35f), 28, TextAnchor.UpperRight);
        textoEnemigos = CrearTextoUI("EnemyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -35f), 28, TextAnchor.UpperLeft);
        textoVida = CrearTextoUI("LifeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -70f), 26, TextAnchor.UpperLeft);
        textoArma = CrearTextoUI("WeaponText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -70f), 26, TextAnchor.UpperRight);
        textoEstado = CrearTextoUI("StatusText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), 28, TextAnchor.UpperCenter);
        textoCuentaRegresiva = CrearTextoUI("CountdownText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), 30, TextAnchor.UpperCenter);
        textoMira = CrearMira();
        textoEstado.color = new Color(1f, 0.95f, 0.8f, 1f);
        textoCuentaRegresiva.color = new Color(1f, 0.82f, 0.35f, 1f);
        AjustarArmaUI();

        overlayDano = CrearOverlayDano();
        panelFinal = CrearPanelFinal();
        panelFinal.SetActive(false);
    }

    void DetenerRutinasVisuales()
    {
        if (rutinaDano != null)
        {
            StopCoroutine(rutinaDano);
            rutinaDano = null;
        }

        if (rutinaEstado != null)
        {
            StopCoroutine(rutinaEstado);
            rutinaEstado = null;
        }

        if (rutinaArma != null)
        {
            StopCoroutine(rutinaArma);
            rutinaArma = null;
        }

        if (rutinaCuentaRegresiva != null)
        {
            StopCoroutine(rutinaCuentaRegresiva);
            rutinaCuentaRegresiva = null;
        }

        if (rutinaCuracion != null)
        {
            StopCoroutine(rutinaCuracion);
            rutinaCuracion = null;
        }
    }

    void LimpiarReferenciasUI()
    {
        canvas = null;
        textoMunicion = null;
        textoEnemigos = null;
        textoEstado = null;
        textoVida = null;
        textoMira = null;
        textoArma = null;
        textoCuentaRegresiva = null;
        overlayDano = null;
        panelFinal = null;
        textoPanel = null;
        botonReintentar = null;
        botonSecundario = null;
        armaRect = null;
        armaImagen = null;
    }

    Text CrearTextoUI(string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 posicion, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(520f, 60f);

        Text texto = go.GetComponent<Text>();
        texto.font = ObtenerFuente();
        texto.fontSize = fontSize;
        texto.alignment = alignment;
        texto.color = Color.white;
        texto.text = string.Empty;

        return texto;
    }

    Text CrearMira()
    {
        GameObject go = new GameObject("CrosshairText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(80f, 80f);

        Text texto = go.GetComponent<Text>();
        texto.font = ObtenerFuente();
        texto.fontSize = 52;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(1f, 1f, 1f, 0.98f);
        texto.text = "+";
        texto.raycastTarget = false;

        return texto;
    }

    void AjustarArmaUI()
    {
        Transform arma = canvas.transform.Find("Arma");
        if (arma == null)
        {
            return;
        }

        RectTransform rect = arma.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-20f, -10f);
        rect.sizeDelta = new Vector2(320f, 220f);
        armaRect = rect;
        armaPosicionBase = rect.anchoredPosition;
        armaTamanoBase = rect.sizeDelta;
        armaImagen = arma.GetComponent<Image>();
    }

    Image CrearOverlayDano()
    {
        GameObject go = new GameObject("DamageOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0f);
        image.raycastTarget = false;

        return image;
    }

    GameObject CrearPanelFinal()
    {
        GameObject panel = new GameObject("PanelFinal", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.78f);

        textoPanel = CrearTextoDentro(panel.transform, "PanelTitle", new Vector2(0f, 120f), new Vector2(700f, 120f), 48, TextAnchor.MiddleCenter);
        botonReintentar = CrearBoton(panel.transform);
        botonSecundario = CrearBotonSecundario(panel.transform);

        return panel;
    }

    Text CrearTextoDentro(Transform padre, string nombre, Vector2 anclado, Vector2 tamano, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anclado;
        rect.sizeDelta = tamano;

        Text texto = go.GetComponent<Text>();
        texto.font = ObtenerFuente();
        texto.fontSize = fontSize;
        texto.alignment = alignment;
        texto.color = Color.white;
        texto.text = string.Empty;

        return texto;
    }

    Button CrearBoton(Transform padre)
    {
        GameObject go = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -80f);
        rect.sizeDelta = new Vector2(280f, 80f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.45f, 0.16f, 1f);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(ReintentarNivel);

        Text texto = CrearTextoDentro(go.transform, "RetryText", Vector2.zero, rect.sizeDelta, 30, TextAnchor.MiddleCenter);
        texto.text = "Reintentar";
        texto.raycastTarget = false;

        return button;
    }

    Button CrearBotonSecundario(Transform padre)
    {
        GameObject go = new GameObject("SecondaryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -180f);
        rect.sizeDelta = new Vector2(280f, 80f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.35f, 0.35f, 0.35f, 1f);

        Button button = go.GetComponent<Button>();

        Text texto = CrearTextoDentro(go.transform, "SecondaryText", Vector2.zero, rect.sizeDelta, 28, TextAnchor.MiddleCenter);
        texto.text = "Cancelar";
        texto.raycastTarget = false;

        return button;
    }

    Font ObtenerFuente()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void PrepararNivelActual(bool reiniciarPosicionJugador)
    {
        if (jugador == null || vidaJugador == null)
        {
            return;
        }

        if (metaActual != null)
        {
            Destroy(metaActual);
        }

        foreach (EnemyAI enemigo in enemigos)
        {
            if (enemigo != null)
            {
                Destroy(enemigo.gameObject);
            }
        }
        enemigos.Clear();

        foreach (GameObject botiquin in botiquines)
        {
            if (botiquin != null)
            {
                Destroy(botiquin);
            }
        }
        botiquines.Clear();

        puntosNivelDisponibles = ObtenerPuntosNavMesh();
        if (puntosNivelDisponibles.Count == 0)
        {
            return;
        }

        if (reiniciarPosicionJugador)
        {
            jugador.position = Vector3.up;
        }

        tiempoInicioNivel = Time.time;

        DatosNivel nivel = niveles[indiceNivelActual];
        int indiceMeta = SeleccionarIndiceMeta(puntosNivelDisponibles);
        metaActual = CrearMeta(puntosNivelDisponibles[indiceMeta]);

        List<Vector3> puntosRestantes = new(puntosNivelDisponibles);
        puntosRestantes.RemoveAt(indiceMeta);

        List<Vector3> puntosEnemigos = SeleccionarPuntosEnCuadrantes(puntosRestantes, 0, nivel.cantidadEnemigos);
        int cantidadEnemigos = puntosEnemigos.Count;
        for (int i = 0; i < cantidadEnemigos; i++)
        {
            CrearEnemigo(puntosEnemigos[i], i + 1, nivel.colorEnemigo);
        }

        List<Vector3> puntosBotiquines = SeleccionarPuntosSeparados(puntosRestantes, cantidadEnemigos, 4, 7f);
        for (int i = 0; i < puntosBotiquines.Count; i++)
        {
            CrearBotiquin(puntosBotiquines[i], i + 1);
        }

        ActualizarNivel();
        ActualizarContadorEnemigos();
        MostrarEstado($"{nivel.nombre}: tienes 5 segundos para atacar primero");
        IniciarCuentaRegresiva();
    }

    int SeleccionarIndiceMeta(List<Vector3> puntos)
    {
        if (puntos.Count <= 1)
        {
            ultimoIndiceMeta = 0;
            return 0;
        }

        int indice = Random.Range(0, puntos.Count);
        if (indice == ultimoIndiceMeta)
        {
            indice = (indice + 1) % puntos.Count;
        }

        ultimoIndiceMeta = indice;
        return indice;
    }

    List<Vector3> SeleccionarPuntosSeparados(List<Vector3> puntos, int inicio, int maximo, float distanciaMinima)
    {
        List<Vector3> seleccionados = new();
        for (int i = inicio; i < puntos.Count && seleccionados.Count < maximo; i++)
        {
            Vector3 candidato = puntos[i];
            bool lejosDeTodos = true;

            foreach (Vector3 existente in seleccionados)
            {
                if (Vector3.Distance(existente, candidato) < distanciaMinima)
                {
                    lejosDeTodos = false;
                    break;
                }
            }

            if (lejosDeTodos)
            {
                seleccionados.Add(candidato);
            }
        }

        if (seleccionados.Count < maximo)
        {
            for (int i = inicio; i < puntos.Count && seleccionados.Count < maximo; i++)
            {
                if (!seleccionados.Contains(puntos[i]))
                {
                    seleccionados.Add(puntos[i]);
                }
            }
        }

        return seleccionados;
    }

    List<Vector3> SeleccionarPuntosEnCuadrantes(List<Vector3> puntos, int inicio, int maximo)
    {
        List<Vector3> seleccionados = new();
        bool[] cuadrantesUsados = new bool[4];

        for (int i = inicio; i < puntos.Count && seleccionados.Count < maximo; i++)
        {
            Vector3 candidato = puntos[i];
            int cuadrante = ObtenerCuadrante(candidato);
            if (cuadrante >= 0 && !cuadrantesUsados[cuadrante])
            {
                seleccionados.Add(candidato);
                cuadrantesUsados[cuadrante] = true;
            }
        }

        if (seleccionados.Count < maximo)
        {
            List<Vector3> extra = SeleccionarPuntosSeparados(puntos, inicio, maximo, 10f);
            foreach (Vector3 punto in extra)
            {
                if (seleccionados.Count >= maximo)
                {
                    break;
                }

                if (!seleccionados.Contains(punto))
                {
                    seleccionados.Add(punto);
                }
            }
        }

        return seleccionados;
    }

    int ObtenerCuadrante(Vector3 punto)
    {
        bool derecha = punto.x >= 0f;
        bool arriba = punto.z >= 0f;

        if (!derecha && arriba) return 0;
        if (derecha && arriba) return 1;
        if (!derecha && !arriba) return 2;
        return 3;
    }

    List<Vector3> ObtenerPuntosNavMesh()
    {
        List<(Vector3 posicion, float distancia)> candidatos = new();
        Vector3[] semillas =
        {
            new Vector3(-22f, 0f, -22f),
            new Vector3(-22f, 0f, -10f),
            new Vector3(-22f, 0f, 0f),
            new Vector3(-22f, 0f, 10f),
            new Vector3(-22f, 0f, 22f),
            new Vector3(-10f, 0f, -22f),
            new Vector3(-10f, 0f, 22f),
            new Vector3(0f, 0f, -22f),
            new Vector3(0f, 0f, 22f),
            new Vector3(10f, 0f, -22f),
            new Vector3(10f, 0f, 22f),
            new Vector3(22f, 0f, -22f),
            new Vector3(22f, 0f, -10f),
            new Vector3(22f, 0f, 0f),
            new Vector3(22f, 0f, 10f),
            new Vector3(22f, 0f, 22f),
            new Vector3(-12f, 0f, -12f),
            new Vector3(12f, 0f, -12f),
            new Vector3(-12f, 0f, 12f),
            new Vector3(12f, 0f, 12f),
            new Vector3(-18f, 0f, 6f),
            new Vector3(18f, 0f, 6f),
            new Vector3(-6f, 0f, 18f),
            new Vector3(6f, 0f, -18f),
        };

        foreach (Vector3 semilla in semillas)
        {
            if (!NavMesh.SamplePosition(semilla, out NavMeshHit hit, 7f, NavMesh.AllAreas))
            {
                continue;
            }

            if (Vector3.Distance(hit.position, jugador.position) < 8f)
            {
                continue;
            }

            if (!EsPuntoUnico(candidatos, hit.position))
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(jugador.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            candidatos.Add((hit.position, CalcularLongitud(path)));
        }

        candidatos.Sort((a, b) => b.distancia.CompareTo(a.distancia));

        List<Vector3> resultado = new();
        foreach ((Vector3 posicion, float _) in candidatos)
        {
            resultado.Add(posicion);
        }

        return resultado;
    }

    bool EsPuntoUnico(List<(Vector3 posicion, float distancia)> candidatos, Vector3 nuevoPunto)
    {
        foreach ((Vector3 posicion, float _) in candidatos)
        {
            if (Vector3.Distance(posicion, nuevoPunto) < 4f)
            {
                return false;
            }
        }

        return true;
    }

    float CalcularLongitud(NavMeshPath path)
    {
        float longitud = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            longitud += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return longitud;
    }

    GameObject CrearMeta(Vector3 posicion)
    {
        GameObject meta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        meta.name = "Meta";
        meta.transform.position = posicion + Vector3.up * 0.25f;
        meta.transform.localScale = new Vector3(2.2f, 0.2f, 2.2f);

        Collider collider = meta.GetComponent<Collider>();
        collider.isTrigger = true;

        Rigidbody rb = meta.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GoalTrigger trigger = meta.AddComponent<GoalTrigger>();
        _ = trigger;

        AplicarColor(meta.GetComponent<Renderer>(), new Color(0.95f, 0.85f, 0.1f, 1f));
        return meta;
    }

    void CrearEnemigo(Vector3 posicion, int indice, Color colorNivel)
    {
        GameObject enemigo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemigo.name = $"Enemigo {indice}";
        enemigo.transform.position = posicion + Vector3.up;
        enemigo.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

        AplicarColor(enemigo.GetComponent<Renderer>(), colorNivel);

        NavMeshAgent agente = enemigo.AddComponent<NavMeshAgent>();
        agente.baseOffset = 1f;

        AudioSource fuente = enemigo.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 1f;

        Vida vida = enemigo.AddComponent<Vida>();
        vida.vidaMax = 2;
        vida.alMorir += OnEnemyDeath;

        EnemyAI ia = enemigo.AddComponent<EnemyAI>();
        ia.Configurar(jugador, vidaJugador, clipDisparoJugador);

        enemigos.Add(ia);
    }

    void CrearBotiquin(Vector3 posicion, int indice)
    {
        GameObject botiquin = new GameObject($"Botiquin {indice}");
        botiquin.transform.position = posicion + Vector3.up * 0.28f;
        botiquin.transform.localScale = Vector3.one * 0.9f;

        Texture2D texturaBotiquin = Resources.Load<Texture2D>("Sprites/botiquin");
        if (texturaBotiquin == null)
        {
            Destroy(botiquin);
            Debug.LogWarning("No se pudo cargar la textura Resources/Sprites/botiquin para crear el botiquin.");
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.transform.SetParent(botiquin.transform, false);
        visual.name = "Visual";
        visual.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        Renderer rendererVisual = visual.GetComponent<Renderer>();
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader);
        material.mainTexture = texturaBotiquin;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texturaBotiquin);
        }
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        rendererVisual.material = material;

        SphereCollider collider = botiquin.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.65f;

        Rigidbody rb = botiquin.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        botiquines.Add(botiquin);
        botiquin.AddComponent<MedkitPickup>();
    }

    void AplicarColor(Renderer renderer, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        renderer.material = material;
    }

    void OnEnemyDeath(Vida vida)
    {
        EnemyAI enemigo = vida.GetComponent<EnemyAI>();
        if (enemigo != null)
        {
            enemigos.Remove(enemigo);
        }

        enemigosEliminadosAcumulados++;
        if (vidaJugador != null && !vidaJugador.EstaMuerto && enemigosEliminadosAcumulados % 2 == 0)
        {
            int curado = vidaJugador.Curar(1);
            if (curado > 0)
            {
                MostrarEstado($"+{curado} vida por eliminar 2 enemigos", 1.5f);
                MostrarFlashCuracion();
            }
        }

        ActualizarContadorEnemigos();

        if (enemigos.Count == 0)
        {
            if (indiceNivelActual < niveles.Length - 1)
            {
                MostrarEstado($"Nivel {indiceNivelActual + 1} limpio. Ve a la meta para pasar");
            }
            else
            {
                MostrarEstado("Todos derrotados. Ve a la meta final");
            }
        }
    }

    void CambiarCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        if (textoMira != null)
        {
            textoMira.gameObject.SetActive(!visible);
        }
    }

    void MostrarPanel(string titulo, string textoBoton)
    {
        if (panelFinal == null)
        {
            return;
        }

        panelFinal.SetActive(true);
        textoPanel.text = titulo;
        Text texto = botonReintentar.GetComponentInChildren<Text>();
        if (texto != null)
        {
            texto.text = textoBoton;
        }

        if (botonSecundario != null)
        {
            botonSecundario.gameObject.SetActive(false);
            botonSecundario.onClick.RemoveAllListeners();
        }
    }

    void ReintentarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool PuedeControlarJugador => estado == EstadoJuego.Jugando;

    public bool PuedeActuarIA => estado == EstadoJuego.Jugando;

    public bool EnemigosPuedenCazarJugador => estado == EstadoJuego.Jugando && Time.time >= tiempoInicioNivel + TiempoCaceriaEnemiga;

    public float TiempoRestanteCaceria => Mathf.Max(0f, (tiempoInicioNivel + TiempoCaceriaEnemiga) - Time.time);

    void ActualizarNivel()
    {
        if (textoCuentaRegresiva == null)
        {
            return;
        }

        textoCuentaRegresiva.text = $"Nivel: {indiceNivelActual + 1} | Cuenta atras: {Mathf.CeilToInt(TiempoRestanteCaceria)}";
    }

    public void ActualizarMunicion(int actual, int maximo, bool recargando)
    {
        if (textoMunicion == null)
        {
            return;
        }

        textoMunicion.text = recargando ? $"Municion: {actual}/{maximo} (Recargando...)" : $"Municion: {actual}/{maximo}";
    }

    public void ActualizarVida(int actual, int maximo)
    {
        if (textoVida == null)
        {
            return;
        }

        textoVida.text = $"Vida: {actual}/{maximo}";
    }

    public void ActualizarArma(string nombre, Vector2 tamanoUI, Color color)
    {
        if (textoArma != null)
        {
            textoArma.text = $"Arma: {nombre}";
        }

        if (armaRect != null)
        {
            armaTamanoBase = tamanoUI;
            armaRect.sizeDelta = tamanoUI;
            armaRect.anchoredPosition = armaPosicionBase;
            armaRect.localRotation = Quaternion.identity;
        }

        if (armaImagen != null)
        {
            armaImagen.color = color;
        }
    }

    public void AnimarDisparoArma()
    {
        if (armaRect == null)
        {
            return;
        }

        if (rutinaArma != null)
        {
            StopCoroutine(rutinaArma);
        }

        rutinaArma = StartCoroutine(RutinaDisparoArma());
    }

    public void AnimarRecargaArma(float duracion)
    {
        if (armaRect == null)
        {
            return;
        }

        if (rutinaArma != null)
        {
            StopCoroutine(rutinaArma);
        }

        rutinaArma = StartCoroutine(RutinaRecargaArma(duracion));
    }

    void ActualizarContadorEnemigos()
    {
        if (textoEnemigos == null)
        {
            return;
        }

        textoEnemigos.text = $"Enemigos: {enemigos.Count}";
    }

    public void MostrarFlashDano()
    {
        if (overlayDano == null)
        {
            return;
        }

        if (rutinaDano != null)
        {
            StopCoroutine(rutinaDano);
        }

        rutinaDano = StartCoroutine(RutinaFlashDano());
    }

    public void MostrarFlashCuracion()
    {
        if (overlayDano == null)
        {
            return;
        }

        if (rutinaCuracion != null)
        {
            StopCoroutine(rutinaCuracion);
        }

        rutinaCuracion = StartCoroutine(RutinaFlashCuracion());
    }

    IEnumerator RutinaFlashDano()
    {
        overlayDano.color = new Color(1f, 0f, 0f, 0.45f);

        float tiempo = 0.2f;
        float transcurrido = 0f;
        while (transcurrido < tiempo)
        {
            transcurrido += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.45f, 0f, transcurrido / tiempo);
            overlayDano.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        overlayDano.color = new Color(1f, 0f, 0f, 0f);
        rutinaDano = null;
    }

    IEnumerator RutinaFlashCuracion()
    {
        overlayDano.color = new Color(0.1f, 1f, 0.2f, 0.35f);

        float tiempo = 0.28f;
        float transcurrido = 0f;
        while (transcurrido < tiempo)
        {
            transcurrido += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.35f, 0f, transcurrido / tiempo);
            overlayDano.color = new Color(0.1f, 1f, 0.2f, alpha);
            yield return null;
        }

        overlayDano.color = new Color(0.1f, 1f, 0.2f, 0f);
        rutinaCuracion = null;
    }

    public void MostrarEstado(string mensaje, float duracion = 0f)
    {
        if (textoEstado == null)
        {
            return;
        }

        textoEstado.text = mensaje;

        if (rutinaEstado != null)
        {
            StopCoroutine(rutinaEstado);
            rutinaEstado = null;
        }

        if (duracion > 0f)
        {
            rutinaEstado = StartCoroutine(LimpiarEstado(duracion));
        }
    }

    void IniciarCuentaRegresiva()
    {
        if (rutinaCuentaRegresiva != null)
        {
            StopCoroutine(rutinaCuentaRegresiva);
        }

        rutinaCuentaRegresiva = StartCoroutine(RutinaCuentaRegresiva());
    }

    IEnumerator LimpiarEstado(float duracion)
    {
        yield return new WaitForSecondsRealtime(duracion);
        if (estado == EstadoJuego.Jugando)
        {
            textoEstado.text = EnemigosPuedenCazarJugador ? "Ahora ellos te estan cazando" : "Tienes 5 segundos para cazarlos primero";
        }
        rutinaEstado = null;
    }

    IEnumerator RutinaCuentaRegresiva()
    {
        while (estado == EstadoJuego.Jugando && TiempoRestanteCaceria > 0f)
        {
            if (textoCuentaRegresiva != null)
            {
                textoCuentaRegresiva.text = $"Nivel: {indiceNivelActual + 1} | Cuenta atras: {Mathf.CeilToInt(TiempoRestanteCaceria)}";
            }
            yield return null;
        }

        if (textoCuentaRegresiva != null)
        {
            textoCuentaRegresiva.text = $"Nivel: {indiceNivelActual + 1} | Cuenta atras: 0";
        }

        if (estado == EstadoJuego.Jugando)
        {
            MostrarEstado("Ahora ellos te estan cazando", 2f);
        }

        rutinaCuentaRegresiva = null;
    }

    IEnumerator RutinaDisparoArma()
    {
        if (armaRect == null)
        {
            rutinaArma = null;
            yield break;
        }

        Vector2 objetivo = armaPosicionBase + new Vector2(-18f, -18f);
        Quaternion rotObjetivo = Quaternion.Euler(0f, 0f, -6f);
        float tiempo = 0.06f;
        float t = 0f;

        while (t < tiempo)
        {
            if (armaRect == null)
            {
                rutinaArma = null;
                yield break;
            }
            t += Time.deltaTime;
            float p = t / tiempo;
            armaRect.anchoredPosition = Vector2.Lerp(armaPosicionBase, objetivo, p);
            armaRect.localRotation = Quaternion.Slerp(Quaternion.identity, rotObjetivo, p);
            yield return null;
        }

        t = 0f;
        while (t < tiempo)
        {
            if (armaRect == null)
            {
                rutinaArma = null;
                yield break;
            }
            t += Time.deltaTime;
            float p = t / tiempo;
            armaRect.anchoredPosition = Vector2.Lerp(objetivo, armaPosicionBase, p);
            armaRect.localRotation = Quaternion.Slerp(rotObjetivo, Quaternion.identity, p);
            yield return null;
        }

        if (armaRect != null)
        {
            armaRect.anchoredPosition = armaPosicionBase;
            armaRect.localRotation = Quaternion.identity;
        }
        rutinaArma = null;
    }

    IEnumerator RutinaRecargaArma(float duracion)
    {
        if (armaRect == null)
        {
            rutinaArma = null;
            yield break;
        }

        Vector2 abajo = armaPosicionBase + new Vector2(120f, -120f);
        Quaternion rotAbajo = Quaternion.Euler(0f, 0f, 24f);
        float mitad = Mathf.Max(duracion * 0.5f, 0.1f);
        float t = 0f;

        while (t < mitad)
        {
            if (armaRect == null)
            {
                rutinaArma = null;
                yield break;
            }
            t += Time.deltaTime;
            float p = t / mitad;
            armaRect.anchoredPosition = Vector2.Lerp(armaPosicionBase, abajo, p);
            armaRect.localRotation = Quaternion.Slerp(Quaternion.identity, rotAbajo, p);
            yield return null;
        }

        t = 0f;
        while (t < mitad)
        {
            if (armaRect == null)
            {
                rutinaArma = null;
                yield break;
            }
            t += Time.deltaTime;
            float p = t / mitad;
            armaRect.anchoredPosition = Vector2.Lerp(abajo, armaPosicionBase, p);
            armaRect.localRotation = Quaternion.Slerp(rotAbajo, Quaternion.identity, p);
            yield return null;
        }

        if (armaRect != null)
        {
            armaRect.anchoredPosition = armaPosicionBase;
            armaRect.localRotation = Quaternion.identity;
        }
        rutinaArma = null;
    }

    public void JugadorMurio()
    {
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

    public void IntentarCompletarNivel()
    {
        if (estado != EstadoJuego.Jugando)
        {
            return;
        }

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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
