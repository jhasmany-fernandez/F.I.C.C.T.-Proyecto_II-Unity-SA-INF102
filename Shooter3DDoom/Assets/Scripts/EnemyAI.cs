using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Vida))]
[RequireComponent(typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    public float velocidad = 3.5f;
    public float radioVision = 14f;
    public float anguloVision = 70f;
    public float rangoAtaque = 12f;
    public float distanciaDetencion = 8f;
    public float cadencia = 1.1f;
    public int dano = 1;
    public float radioPatrulla = 6f;
    public float pausaPatrulla = 1.2f;

    private Transform objetivo;
    private Vida vidaObjetivo;
    private NavMeshAgent agente;
    private AudioSource fuente;
    private AudioClip sonidoDisparo;
    private Vector3 origenPatrulla;
    private float proximoDisparo;
    private float finPausaPatrulla;
    private bool persiguiendo;
    private Coroutine efectoDisparoRutina;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        fuente = GetComponent<AudioSource>();
    }

    void Start()
    {
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

        bool puedeCazar = JuegoManager.Instance == null || JuegoManager.Instance.EnemigosPuedenCazarJugador;
        bool puedeVerJugador = PuedeVerJugador();
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
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 8f);
        }

        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (persiguiendo && distancia <= rangoAtaque && Time.time >= proximoDisparo && TieneLineaDeVision())
        {
            proximoDisparo = Time.time + cadencia;
            Disparar();
        }
    }

    Vector3 ObtenerDireccionMirada()
    {
        if (persiguiendo && objetivo != null)
        {
            Vector3 direccionObjetivo = objetivo.position - transform.position;
            direccionObjetivo.y = 0f;
            return direccionObjetivo;
        }

        Vector3 velocidadActual = agente.desiredVelocity;
        velocidadActual.y = 0f;
        return velocidadActual;
    }

    bool PuedeVerJugador()
    {
        if (objetivo == null)
        {
            return false;
        }

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

        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, rangoAtaque + 1f))
        {
            return hit.collider.GetComponentInParent<Vida>() == vidaObjetivo;
        }

        return false;
    }

    void ActualizarPatrulla()
    {
        if (Time.time < finPausaPatrulla)
        {
            agente.ResetPath();
            return;
        }

        if (!agente.hasPath || agente.remainingDistance <= agente.stoppingDistance + 0.2f)
        {
            finPausaPatrulla = Time.time + pausaPatrulla;
            ElegirPuntoPatrulla();
        }
    }

    void ElegirPuntoPatrulla()
    {
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
        if (sonidoDisparo != null)
        {
            fuente.PlayOneShot(sonidoDisparo);
        }

        Vector3 origen = transform.position + Vector3.up * 1.2f;
        Vector3 destino = objetivo.position + Vector3.up * 0.6f;
        Vector3 direccion = (destino - origen).normalized;

        if (Physics.Raycast(origen, direccion, out RaycastHit hit, rangoAtaque + 1f))
        {
            if (efectoDisparoRutina != null)
            {
                StopCoroutine(efectoDisparoRutina);
            }
            efectoDisparoRutina = StartCoroutine(MostrarLineaDisparo(origen, hit.point));

            Vida vidaImpactada = hit.collider.GetComponentInParent<Vida>();
            if (vidaImpactada == vidaObjetivo)
            {
                vidaObjetivo.RecibirDano(dano);
            }
        }
    }

    System.Collections.IEnumerator MostrarLineaDisparo(Vector3 origen, Vector3 destino)
    {
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
