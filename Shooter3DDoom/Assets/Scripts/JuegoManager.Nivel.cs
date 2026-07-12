using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

// Agrupa generacion procedural del nivel, meta, enemigos y botiquines.
public partial class JuegoManager
{
    void ConfigurarNavMesh()
    {
        try
        {
            // Busca el piso principal y le asegura una superficie navegable para la IA.
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
            // Se reconstruye al cargar la escena para que la IA use el mapa actual.
            surface.BuildNavMesh();
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(ConfigurarNavMesh), ex);
        }
    }

    void PrepararNivelActual(bool reiniciarPosicionJugador)
    {
        try
        {
            // Genera desde cero el contenido del nivel actual.
            if (jugador == null || vidaJugador == null)
            {
                return;
            }

            // Limpia la poblacion del nivel anterior antes de generar la nueva ronda.
            if (metaActual != null)
            {
                Destroy(metaActual);
            }

        foreach (EnemigoIA enemigo in enemigos)
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

            // Reserva la posicion de la meta para que no se superponga con enemigos ni botiquines.
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
        catch (System.Exception ex)
        {
            RegistrarError(nameof(PrepararNivelActual), ex);
        }
    }

    int SeleccionarIndiceMeta(List<Vector3> puntos)
    {
        // Selecciona una posicion para la meta evitando repetir la anterior si es posible.
        if (puntos.Count <= 1)
        {
            ultimoIndiceMeta = 0;
            return 0;
        }

        // Evita repetir inmediatamente la misma meta entre niveles consecutivos.
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
        // Filtra puntos que respeten una separacion minima entre si.
        List<Vector3> seleccionados = new();
        for (int i = inicio; i < puntos.Count && seleccionados.Count < maximo; i++)
        {
            Vector3 candidato = puntos[i];
            bool lejosDeTodos = true;

            // Fuerza distribucion minima entre objetos para que no aparezcan amontonados.
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
        // Prioriza repartir enemigos por cuadrantes distintos del mapa.
        List<Vector3> seleccionados = new();
        bool[] cuadrantesUsados = new bool[4];

        // Intenta repartir enemigos por zonas distintas del mapa antes de completar extras.
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
        // Convierte una posicion del mapa en uno de cuatro cuadrantes logicos.
        bool derecha = punto.x >= 0f;
        bool arriba = punto.z >= 0f;

        if (!derecha && arriba) return 0;
        if (derecha && arriba) return 1;
        if (!derecha && !arriba) return 2;
        return 3;
    }

    List<Vector3> ObtenerPuntosNavMesh()
    {
        // Construye una lista de puntos navegables candidatos sobre el mapa.
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

            // Descarta puntos demasiado cercanos al jugador y conserva solo puntos navegables.
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

        // Ordena por longitud de camino para priorizar puntos interesantes del mapa.
        List<Vector3> resultado = new();
        foreach ((Vector3 posicion, float _) in candidatos)
        {
            resultado.Add(posicion);
        }

        return resultado;
    }

    bool EsPuntoUnico(List<(Vector3 posicion, float distancia)> candidatos, Vector3 nuevoPunto)
    {
        // Evita guardar puntos practicamente repetidos en la lista final.
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
        // Suma la longitud real del camino calculado por NavMesh.
        float longitud = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            longitud += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return longitud;
    }

    GameObject CrearMeta(Vector3 posicion)
    {
        // Crea el objeto fisico de la meta con trigger y color amarillo.
        GameObject meta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        meta.name = "Meta";
        meta.transform.position = posicion + Vector3.up * 0.25f;
        meta.transform.localScale = new Vector3(2.2f, 0.2f, 2.2f);

        Collider collider = meta.GetComponent<Collider>();
        collider.isTrigger = true;

        Rigidbody rb = meta.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        ActivadorMeta trigger = meta.AddComponent<ActivadorMeta>();
        _ = trigger;

        AplicarColor(meta.GetComponent<Renderer>(), new Color(0.95f, 0.85f, 0.1f, 1f));
        return meta;
    }

    void CrearEnemigo(Vector3 posicion, int indice, Color colorNivel)
    {
        // Construye cada enemigo por codigo y le conecta IA, vida, audio y NavMesh.
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

        // Crea un arma frontal visible que sobresalga del cuerpo del enemigo.
        GameObject arma = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arma.name = "ArmaEnemigo";
        arma.transform.SetParent(enemigo.transform, false);
        arma.transform.localPosition = new Vector3(0.42f, 0.18f, 0.82f);
        arma.transform.localRotation = Quaternion.Euler(-8f, 18f, -24f);
        arma.transform.localScale = new Vector3(0.24f, 0.16f, 0.8f);
        AplicarColor(arma.GetComponent<Renderer>(), new Color(0.08f, 0.08f, 0.08f, 1f));

        Collider colliderArma = arma.GetComponent<Collider>();
        if (colliderArma != null)
        {
            Destroy(colliderArma);
        }

        // Punta luminosa para que se note claramente donde esta el arma.
        GameObject boquilla = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boquilla.name = "BoquillaArma";
        boquilla.transform.SetParent(arma.transform, false);
        boquilla.transform.localPosition = new Vector3(0f, 0f, 0.52f);
        boquilla.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        AplicarColor(boquilla.GetComponent<Renderer>(), new Color(1f, 0.35f, 0.1f, 1f));

        Collider colliderBoquilla = boquilla.GetComponent<Collider>();
        if (colliderBoquilla != null)
        {
            Destroy(colliderBoquilla);
        }

        // Punto exacto desde donde nace el rayo del enemigo.
        GameObject puntoDisparo = new GameObject("PuntoDisparo");
        puntoDisparo.transform.SetParent(arma.transform, false);
        puntoDisparo.transform.localPosition = new Vector3(0f, 0f, 0.56f);

        Vida vida = enemigo.AddComponent<Vida>();
        vida.vidaMax = 2;
        vida.alMorir += OnEnemyDeath;

        EnemigoIA ia = enemigo.AddComponent<EnemigoIA>();
        ia.Configurar(jugador, vidaJugador, clipDisparoJugador);

        enemigos.Add(ia);
    }

    void CrearBotiquin(Vector3 posicion, int indice)
    {
        try
        {
            // Crea un botiquin visual usando la textura exigida por el enunciado.
            GameObject botiquin = new GameObject($"Botiquin {indice}");
            botiquin.transform.position = posicion + Vector3.up * 0.28f;
            botiquin.transform.localScale = Vector3.one * 0.9f;

            // El botiquin obligatorio se representa con la textura pedida en el enunciado.
            Texture2D texturaBotiquin = Resources.Load<Texture2D>("Sprites/botiquin");
            if (texturaBotiquin == null)
            {
                Destroy(botiquin);
                Debug.LogWarning("No se pudo cargar la textura Resources/Sprites/botiquin para crear el botiquin.");
                return;
            }

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(botiquin.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Convierte la textura en un Sprite en tiempo de ejecucion para usar un SpriteRenderer real.
            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            Sprite spriteBotiquin = Sprite.Create(
                texturaBotiquin,
                new Rect(0f, 0f, texturaBotiquin.width, texturaBotiquin.height),
                new Vector2(0.5f, 0.5f),
                100f);
            spriteRenderer.sprite = spriteBotiquin;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 5;

            SphereCollider collider = botiquin.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.65f;

            Rigidbody rb = botiquin.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            botiquines.Add(botiquin);
            botiquin.AddComponent<RecogerBotiquin>();
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(CrearBotiquin), ex);
        }
    }

    void AplicarColor(Renderer renderer, Color color)
    {
        // Helper para aplicar color tanto en URP como en shaders legacy.
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
        // Al morir un enemigo, se actualiza la lista, HUD y posibles recompensas.
        EnemigoIA enemigo = vida.GetComponent<EnemigoIA>();
        if (enemigo != null)
        {
            enemigos.Remove(enemigo);
        }

        // Cada dos bajas, el jugador recibe una pequena recompensa de vida.
        // Lleva la cuenta total de enemigos eliminados desde que empezo el nivel actual.
        enemigosEliminadosAcumulados++;

        // La recompensa se activa solo cuando:
        // 1. existe el jugador,
        // 2. el jugador sigue vivo,
        // 3. y la cantidad de enemigos eliminados es multiplo de 2.
        // El operador % 2 == 0 significa "cada dos enemigos".
        if (vidaJugador != null && !vidaJugador.EstaMuerto && enemigosEliminadosAcumulados % 2 == 0)
        {
            // Intenta curar exactamente 1 punto de vida.
            // El metodo Curar ya respeta el limite maximo de vida del jugador.
            int curado = vidaJugador.Curar(1);

            // Solo muestra el mensaje y el efecto visual si realmente recupero vida.
            // Si el jugador ya estaba al maximo, Curar devuelve 0 y no se muestra recompensa falsa.
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
}
