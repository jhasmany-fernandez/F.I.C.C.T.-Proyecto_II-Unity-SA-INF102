using UnityEngine;

public class MedkitPickup : MonoBehaviour
{
    public int curacion = 1;

    private static AudioClip clipPickup;
    private float tiempoBase;
    private Vector3 posicionBase;

    void Awake()
    {
        if (clipPickup == null)
        {
            clipPickup = CrearSonidoPickup();
        }

        tiempoBase = Random.Range(0f, Mathf.PI * 2f);
        posicionBase = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);

        Vector3 posicion = posicionBase;
        posicion.y += Mathf.Sin(Time.time * 2.5f + tiempoBase) * 0.08f;
        transform.position = posicion;

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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Vida vida = other.GetComponent<Vida>();
        if (vida == null)
        {
            return;
        }

        int recuperado = vida.Curar(curacion);
        if (recuperado <= 0)
        {
            return;
        }

        if (clipPickup != null)
        {
            AudioSource.PlayClipAtPoint(clipPickup, transform.position, 0.75f);
        }

        JuegoManager.Instance?.MostrarEstado($"+{recuperado} vida", 1.2f);
        Destroy(gameObject);
    }

    static AudioClip CrearSonidoPickup()
    {
        const int sampleRate = 44100;
        const float duracion = 0.18f;
        int samples = Mathf.CeilToInt(sampleRate * duracion);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (t / duracion);
            float wave = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.5f;
            wave += Mathf.Sin(2f * Mathf.PI * 1080f * t) * 0.25f;
            data[i] = wave * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("PickupMedkit", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
