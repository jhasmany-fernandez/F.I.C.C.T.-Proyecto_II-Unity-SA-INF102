using UnityEngine;
using UnityEngine.AI;

// Garantiza que el enemigo tenga los componentes necesarios para navegar, recibir dano y sonar.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Vida))]
[RequireComponent(typeof(AudioSource))]
// Implementa la IA basica del enemigo: patrulla, deteccion, persecucion y disparo.
public class EnemyAI : MonoBehaviour
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

    void Awake()
    {
        // Cachea componentes para no buscarlos cada frame.
        agente = GetComponent<NavMeshAgent>();
        fuente = GetComponent<AudioSource>();
    }

    void Start()
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

    public void Configurar(Transform nuevoObjetivo, Vida nuevaVidaObjetivo, AudioClip clipDisparo)
    {
        // Recibe desde el gestor las referencias del jugador que debe perseguir.
        objetivo = nuevoObjetivo;
        vidaObjetivo = nuevaVidaObjetivo;
        sonidoDisparo = clipDisparo;
    }

    void Update()
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
        Vector3 origen = transform.position + Vector3.up * 1.2f;
        Vector3 destino = objetivo.position + Vector3.up * 0.6f;
        Vector3 direccion = destino - origen;

        // El raycast evita que el enemigo dispare a traves de paredes.
        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, rangoAtaque + 1f))
        {
            return hit.collider.GetComponentInParent<Vida>() == vidaObjetivo;
        }

        return false;
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
        // Reproduce el sonido si el enemigo tiene clip configurado.
        if (sonidoDisparo != null)
        {
            fuente.PlayOneShot(sonidoDisparo);
        }

        // El ataque usa raycast para comportarse de forma parecida al disparo del jugador.
        Vector3 origen = transform.position + Vector3.up * 1.2f;
        Vector3 destino = objetivo.position + Vector3.up * 0.6f;
        Vector3 direccion = (destino - origen).normalized;

        if (Physics.Raycast(origen, direccion, out RaycastHit hit, rangoAtaque + 1f))
        {
            // Reinicia el efecto visual si habia uno anterior activo.
            if (efectoDisparoRutina != null)
            {
                StopCoroutine(efectoDisparoRutina);
            }
            efectoDisparoRutina = StartCoroutine(MostrarLineaDisparo(origen, hit.point));

            Vida vidaImpactada = hit.collider.GetComponentInParent<Vida>();
            if (vidaImpactada == vidaObjetivo)
            {
                // Solo aplica dano si realmente impacto al jugador.
                vidaObjetivo.RecibirDano(dano);
            }
        }
    }

    System.Collections.IEnumerator MostrarLineaDisparo(Vector3 origen, Vector3 destino)
    {
        // Traza visual corta para que el disparo enemigo sea visible en pantalla.
        GameObject linea = new GameObject("DisparoEnemigo");
        LineRenderer lineRenderer = linea.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origen);
        lineRenderer.SetPosition(1, destino);
        lineRenderer.startWidth = 0.06f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = new Color(1f, 0.3f, 0.2f, 0.95f);
        lineRenderer.material = material;
        lineRenderer.startColor = material.color;
        lineRenderer.endColor = new Color(1f, 0.9f, 0.2f, 0.2f);

        yield return new WaitForSeconds(0.06f);

        Destroy(linea);
        efectoDisparoRutina = null;
    }
}
