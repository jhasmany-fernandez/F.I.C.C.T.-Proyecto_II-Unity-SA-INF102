using UnityEngine;
using UnityEngine.AI;

// Garantiza que el enemigo tenga los componentes necesarios para navegar, recibir dano y sonar.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Vida))]
[RequireComponent(typeof(AudioSource))]
// Implementa la IA basica del enemigo: patrulla, deteccion, persecucion y disparo.
public class EnemigoIA : MonoBehaviour
{
    // Velocidad base de desplazamiento.
    public float velocidad = 3.5f;
    // Distancia maxima para detectar al jugador.
    public float radioVision = 14f;
    // Angulo del cono de vision.
    public float anguloVision = 70f;
    // Distancia maxima para atacar.
    public float rangoAtaque = 12f;
    // Distancia a la que deja de acercarse al jugador.
    public float distanciaDetencion = 8f;
    // Tiempo minimo entre disparos.
    public float cadencia = 1.1f;
    // Dano por disparo.
    public int dano = 1;
    // Radio usado para elegir puntos de patrulla.
    public float radioPatrulla = 6f;
    // Tiempo de pausa antes de escoger otro punto de patrulla.
    public float pausaPatrulla = 1.2f;

    // Referencia al transform del jugador.
    private Transform objetivo;
    // Referencia al sistema de vida del jugador.
    private Vida vidaObjetivo;
    // Agente NavMesh usado para moverse por el mapa.
    private NavMeshAgent agente;
    // Fuente de audio para el disparo enemigo.
    private AudioSource fuente;
    // Sonido compartido del disparo.
    private AudioClip sonidoDisparo;
    // Collider del jugador para apuntar al centro real del cuerpo y validar impactos.
    private Collider colliderObjetivo;
    // Punto visual desde el que nace el disparo enemigo.
    private Transform puntoDisparo;
    // Punto central desde el que patrulla el enemigo.
    private Vector3 origenPatrulla;
    // Proximo instante valido para disparar.
    private float proximoDisparo;
    // Tiempo hasta el que debe esperar quieto antes de retomar patrulla.
    private float finPausaPatrulla;
    // Marca si el enemigo ya esta siguiendo al jugador.
    private bool persiguiendo;
    // Permite reiniciar el efecto visual del disparo si dispara muy seguido.
    private Coroutine efectoDisparoRutina;

    void RegistrarError(string contexto, System.Exception ex)
    {
        Debug.LogError($"[EnemigoIA] Error en {contexto}: {ex.Message}\n{ex}", this);
    }

    void Awake()
    {
        try
        {
            // Cachea componentes para no buscarlos cada frame.
            agente = GetComponent<NavMeshAgent>();
            fuente = GetComponent<AudioSource>();
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Awake), ex);
        }
    }

    void Start()
    {
        try
        {
            // Configura la navegacion inicial del enemigo.
            origenPatrulla = transform.position;
            agente.speed = velocidad;
            agente.stoppingDistance = distanciaDetencion;
            agente.angularSpeed = 360f;
            agente.acceleration = 12f;
            agente.updateRotation = false;
            ElegirPuntoPatrulla();
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Start), ex);
        }
    }

    public void Configurar(Transform nuevoObjetivo, Vida nuevaVidaObjetivo, AudioClip clipDisparo)
    {
        // Recibe desde el gestor las referencias del jugador que debe perseguir.
        objetivo = nuevoObjetivo;
        vidaObjetivo = nuevaVidaObjetivo;
        sonidoDisparo = clipDisparo;
        colliderObjetivo = objetivo != null ? objetivo.GetComponent<Collider>() : null;
        puntoDisparo = transform.Find("PuntoDisparo");
    }

    void Update()
    {
        try
        {
            if (JuegoManager.Instance != null && !JuegoManager.Instance.PuedeActuarIA)
            {
                agente.isStopped = true;
                return;
            }

            if (objetivo == null || vidaObjetivo == null || vidaObjetivo.EstaMuerto)
            {
                return;
            }

            // Decide si debe perseguir porque ya empezo la caceria o porque vio al jugador.
            bool puedeCazar = JuegoManager.Instance == null || JuegoManager.Instance.EnemigosPuedenCazarJugador;
            bool puedeVerJugador = PuedeVerJugador();
            // Primero patrulla; despues de la cuenta regresiva todos los enemigos entran en caceria.
            if (puedeCazar)
            {
                persiguiendo = true;
            }
            else if (puedeVerJugador)
            {
                persiguiendo = true;
            }
            else if (persiguiendo && !TieneLineaDeVision() && Vector3.Distance(transform.position, objetivo.position) > radioVision * 1.35f)
            {
                persiguiendo = false;
                finPausaPatrulla = Time.time + pausaPatrulla;
            }

            agente.isStopped = false;
            // Si esta persiguiendo sigue al jugador; si no, recorre su patrulla local.
            if (persiguiendo)
            {
                agente.SetDestination(objetivo.position);
            }
            else
            {
                ActualizarPatrulla();
            }

            Vector3 direccion = ObtenerDireccionMirada();
            if (direccion.sqrMagnitude > 0.01f)
            {
                // Suaviza la rotacion para que siempre mire hacia donde se mueve o dispara.
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 8f);
            }

            // Solo dispara cuando esta cerca, tiene cadencia disponible y linea de vision.
            float distancia = Vector3.Distance(transform.position, objetivo.position);
            if (persiguiendo && distancia <= rangoAtaque && Time.time >= proximoDisparo && TieneLineaDeVision())
            {
                proximoDisparo = Time.time + cadencia;
                Disparar();
            }
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Update), ex);
        }
    }

    Vector3 ObtenerDireccionMirada()
    {
        // Mientras persigue, prioriza mirar al jugador.
        if (persiguiendo && objetivo != null)
        {
            Vector3 direccionObjetivo = objetivo.position - transform.position;
            direccionObjetivo.y = 0f;
            return direccionObjetivo;
        }

        // Si esta patrullando, gira hacia la direccion deseada por el agente.
        Vector3 velocidadActual = agente.desiredVelocity;
        velocidadActual.y = 0f;
        return velocidadActual;
    }

    bool PuedeVerJugador()
    {
        // Sin objetivo no hay deteccion posible.
        if (objetivo == null)
        {
            return false;
        }

        // Primer filtro: distancia maxima de vision.
        Vector3 haciaJugador = objetivo.position - transform.position;
        float distancia = haciaJugador.magnitude;
        if (distancia > radioVision)
        {
            return false;
        }

        Vector3 plano = new Vector3(haciaJugador.x, 0f, haciaJugador.z);
        if (plano.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        // Segundo filtro: cono de vision segun el frente del enemigo.
        float angulo = Vector3.Angle(transform.forward, plano.normalized);
        if (angulo > anguloVision)
        {
            return false;
        }

        return TieneLineaDeVision();
    }

    bool TieneLineaDeVision()
    {
        // Solo hay vision si el primer collider solido en la linea recta pertenece al jugador.
        return TryGetPrimerImpactoVisible(out RaycastHit hit) && hit.collider.GetComponentInParent<Vida>() == vidaObjetivo;
    }

    void ActualizarPatrulla()
    {
        // Durante la pausa no busca un nuevo destino.
        if (Time.time < finPausaPatrulla)
        {
            agente.ResetPath();
            return;
        }

        // Si llego o no tiene camino, selecciona otro punto cercano.
        if (!agente.hasPath || agente.remainingDistance <= agente.stoppingDistance + 0.2f)
        {
            finPausaPatrulla = Time.time + pausaPatrulla;
            ElegirPuntoPatrulla();
        }
    }

    void ElegirPuntoPatrulla()
    {
        // Intenta varias veces encontrar un punto valido alrededor del origen.
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * radioPatrulla;
            Vector3 candidato = origenPatrulla + new Vector3(offset2D.x, 0f, offset2D.y);
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agente.SetDestination(hit.position);
                return;
            }
        }

        agente.SetDestination(origenPatrulla);
    }

    void Disparar()
    {
        try
        {
            // Reproduce el sonido si el enemigo tiene clip configurado.
            if (sonidoDisparo != null && fuente != null)
            {
                fuente.PlayOneShot(sonidoDisparo);
            }

            // El ataque usa la misma validacion de linea de vision para no atravesar paredes.
            if (TryGetPrimerImpactoVisible(out RaycastHit hit))
            {
                // Reinicia el efecto visual si habia uno anterior activo.
                if (efectoDisparoRutina != null)
                {
                    StopCoroutine(efectoDisparoRutina);
                }
                efectoDisparoRutina = StartCoroutine(MostrarLineaDisparo(ObtenerOrigenDisparo(), hit.point));

                Vida vidaImpactada = hit.collider.GetComponentInParent<Vida>();
                if (vidaImpactada == vidaObjetivo)
                {
                    // Solo aplica dano si realmente impacto al jugador.
                    vidaObjetivo.RecibirDano(dano);
                }
            }
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Disparar), ex);
        }
    }

    bool TryGetPrimerImpactoVisible(out RaycastHit hit)
    {
        Vector3 origen = ObtenerOrigenDisparo();
        Vector3 destino = ObtenerPuntoObjetivo();
        Vector3 direccion = destino - origen;
        float distanciaObjetivo = direccion.magnitude;

        // Si el objetivo esta demasiado cerca, no hace falta raycast.
        if (distanciaObjetivo <= 0.01f)
        {
            hit = default;
            return false;
        }

        // Usa la distancia exacta al jugador e ignora triggers para que una pared siempre bloquee el disparo.
        return Physics.Raycast(
            origen,
            direccion.normalized,
            out hit,
            distanciaObjetivo,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    Vector3 ObtenerOrigenDisparo()
    {
        // Si existe el punto del arma, el disparo nace exactamente desde ahi.
        if (puntoDisparo != null)
        {
            return puntoDisparo.position;
        }

        // Fallback por si el punto visual no se creo correctamente.
        return transform.position + Vector3.up * 1.05f + transform.forward * 0.3f;
    }

    Vector3 ObtenerPuntoObjetivo()
    {
        // Si el jugador tiene collider, apunta al centro del cuerpo para una linea de tiro mas fiable.
        if (colliderObjetivo != null)
        {
            return colliderObjetivo.bounds.center;
        }

        return objetivo.position + Vector3.up * 0.6f;
    }

    System.Collections.IEnumerator MostrarLineaDisparo(Vector3 origen, Vector3 destino)
    {
        // Traza visual corta y brillante para que el disparo enemigo parezca un rayo.
        GameObject linea = new GameObject("DisparoEnemigo");
        LineRenderer lineRenderer = linea.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origen);
        lineRenderer.SetPosition(1, destino);
        lineRenderer.startWidth = 0.12f;
        lineRenderer.endWidth = 0.04f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.numCapVertices = 6;
        lineRenderer.alignment = LineAlignment.View;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = new Color(1f, 0.2f, 0.1f, 1f);
        lineRenderer.material = material;
        lineRenderer.startColor = material.color;
        lineRenderer.endColor = new Color(1f, 0.95f, 0.35f, 0.5f);

        yield return new WaitForSeconds(0.09f);

        Destroy(linea);
        efectoDisparoRutina = null;
    }
}
