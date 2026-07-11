using UnityEngine;

// Maneja el comportamiento de recoger un botiquin, curar y reproducir sonido.
public class MedkitPickup : MonoBehaviour
{
    // Cantidad de vida que devuelve cada botiquin.
    public int curacion = 1;

    // Se comparte entre todos los botiquines para no recrear el audio cada vez.
    private static AudioClip clipPickup;
    // Desfase aleatorio para que cada botiquin flote distinto.
    private float tiempoBase;
    // Posicion base usada para el efecto de flotacion.
    private Vector3 posicionBase;

    void RegistrarError(string contexto, System.Exception ex)
    {
        Debug.LogError($"[MedkitPickup] Error en {contexto}: {ex.Message}\n{ex}", this);
    }

    void Awake()
    {
        try
        {
            // Crea una sola vez el sonido sintetico del pickup.
            if (clipPickup == null)
            {
                clipPickup = CrearSonidoPickup();
            }

            // Guarda una fase distinta para evitar animaciones sincronizadas.
            tiempoBase = Random.Range(0f, Mathf.PI * 2f);
            posicionBase = transform.position;
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Awake), ex);
        }
    }

    void Update()
    {
        try
        {
            // Gira lentamente el botiquin para hacerlo mas visible.
            transform.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);

            // Aplica una oscilacion vertical suave.
            Vector3 posicion = posicionBase;
            posicion.y += Mathf.Sin(Time.time * 2.5f + tiempoBase) * 0.08f;
            transform.position = posicion;

            // Hace que la imagen siempre mire hacia la camara principal.
            Transform visual = transform.Find("Visual");
            if (visual != null && Camera.main != null)
            {
                Vector3 direccion = Camera.main.transform.position - visual.position;
                direccion.y = 0f;
                if (direccion.sqrMagnitude > 0.001f)
                {
                    visual.rotation = Quaternion.LookRotation(-direccion.normalized, Vector3.up);
                }
            }
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(Update), ex);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        try
        {
            // Solo el jugador puede recoger el botiquin.
            if (!other.CompareTag("Player"))
            {
                return;
            }

            // Busca el componente de vida del jugador que entro al trigger.
            Vida vida = other.GetComponent<Vida>();
            if (vida == null)
            {
                return;
            }

            // Si la vida ya esta llena, no consume el botiquin.
            int recuperado = vida.Curar(curacion);
            if (recuperado <= 0)
            {
                return;
            }

            // Reproduce el sonido en la posicion del pickup antes de destruirlo.
            if (clipPickup != null)
            {
                AudioSource.PlayClipAtPoint(clipPickup, transform.position, 0.75f);
            }

            JuegoManager.Instance?.MostrarEstado($"+{recuperado} vida", 1.2f);
            Destroy(gameObject);
        }
        catch (System.Exception ex)
        {
            RegistrarError(nameof(OnTriggerEnter), ex);
        }
    }

    static AudioClip CrearSonidoPickup()
    {
        // Genera un sonido corto por codigo para no depender de un archivo externo.
        const int sampleRate = 44100;
        const float duracion = 0.18f;
        int samples = Mathf.CeilToInt(sampleRate * duracion);
        float[] data = new float[samples];

        // Mezcla dos senoidales con una envolvente decreciente para simular un pickup.
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (t / duracion);
            float wave = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.5f;
            wave += Mathf.Sin(2f * Mathf.PI * 1080f * t) * 0.25f;
            data[i] = wave * envelope * 0.25f;
        }

        // Crea el AudioClip final y copia los samples sintetizados.
        AudioClip clip = AudioClip.Create("PickupMedkit", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
