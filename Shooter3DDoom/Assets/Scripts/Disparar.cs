using UnityEngine;

public class Disparar : MonoBehaviour
{
    [System.Serializable]
    class DatosArma
    {
        public string nombre;
        public int dano;
        public float alcance;
        public float cadencia;
        public int balasPorCargador;
        public float tiempoRecarga;
        public Vector2 escalaUI;
        public Color colorUI;
    }

    public Camera camara;
    public AudioClip sonidoDisparo;
    public GameObject muzzle;

    private readonly DatosArma[] armas =
    {
        new DatosArma
        {
            nombre = "Pistola",
            dano = 2,
            alcance = 100f,
            cadencia = 0.5f,
            balasPorCargador = 8,
            tiempoRecarga = 1.5f,
            escalaUI = new Vector2(320f, 220f),
            colorUI = Color.white,
        },
        new DatosArma
        {
            nombre = "Rifle",
            dano = 4,
            alcance = 140f,
            cadencia = 0.18f,
            balasPorCargador = 18,
            tiempoRecarga = 1.1f,
            escalaUI = new Vector2(420f, 210f),
            colorUI = new Color(0.8f, 1f, 0.8f, 1f),
        }
    };

    private AudioSource fuente;
    private float proximo = 0f;
    private int balasActuales;
    private bool recargando;
    private int indiceArmaActual;
    private Coroutine muzzleRutina;
    private Coroutine recargaRutina;

    public int BalasActuales => balasActuales;

    public int BalasPorCargador => ArmaActual.balasPorCargador;

    public bool EstaRecargando => recargando;

    public string NombreArmaActual => ArmaActual.nombre;

    DatosArma ArmaActual => armas[indiceArmaActual];

    void Start()
    {
        fuente = GetComponent<AudioSource>();
        balasActuales = ArmaActual.balasPorCargador;
        if (muzzle != null) muzzle.SetActive(false);
        JuegoManager.Instance?.ActualizarArma(NombreArmaActual, ArmaActual.escalaUI, ArmaActual.colorUI);
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
    }

    void Update()
    {
        if (JuegoManager.Instance != null && !JuegoManager.Instance.PuedeControlarJugador)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CambiarArma(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CambiarArma(1);
        }

        if (Input.GetKeyDown(KeyCode.R) && balasActuales < ArmaActual.balasPorCargador)
        {
            IniciarRecarga();
        }

        if (recargando)
        {
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= proximo)
        {
            if (balasActuales <= 0)
            {
                IniciarRecarga();
                return;
            }

            proximo = Time.time + ArmaActual.cadencia;
            balasActuales--;
            JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
            JuegoManager.Instance?.AnimarDisparoArma();
            Disparo();

            if (balasActuales <= 0)
            {
                IniciarRecarga();
            }
        }
    }

    void Disparo()
    {
        if (sonidoDisparo != null) fuente.PlayOneShot(sonidoDisparo);

        if (muzzle != null)
        {
            muzzle.SetActive(true);
            if (muzzleRutina != null)
            {
                StopCoroutine(muzzleRutina);
            }
            muzzleRutina = StartCoroutine(ApagarMuzzle());
        }

        Ray ray = camara.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, ArmaActual.alcance))
        {
            Vida v = hit.collider.GetComponentInParent<Vida>();
            if (v != null) v.RecibirDano(ArmaActual.dano);
        }
    }

    void CambiarArma(int nuevoIndice)
    {
        if (nuevoIndice < 0 || nuevoIndice >= armas.Length || nuevoIndice == indiceArmaActual)
        {
            return;
        }

        indiceArmaActual = nuevoIndice;
        recargando = false;
        if (recargaRutina != null)
        {
            StopCoroutine(recargaRutina);
            recargaRutina = null;
        }

        balasActuales = ArmaActual.balasPorCargador;
        JuegoManager.Instance?.ActualizarArma(NombreArmaActual, ArmaActual.escalaUI, ArmaActual.colorUI);
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
        JuegoManager.Instance?.MostrarEstado($"Arma equipada: {NombreArmaActual}", 1.2f);
    }

    void IniciarRecarga()
    {
        if (recargando || balasActuales == ArmaActual.balasPorCargador)
        {
            return;
        }

        if (recargaRutina != null)
        {
            StopCoroutine(recargaRutina);
        }

        recargaRutina = StartCoroutine(RecargarRutina());
    }

    System.Collections.IEnumerator RecargarRutina()
    {
        recargando = true;
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
        JuegoManager.Instance?.AnimarRecargaArma(ArmaActual.tiempoRecarga);
        yield return new WaitForSeconds(ArmaActual.tiempoRecarga);
        balasActuales = ArmaActual.balasPorCargador;
        recargando = false;
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
        recargaRutina = null;
    }

    System.Collections.IEnumerator ApagarMuzzle()
    {
        yield return new WaitForSeconds(0.05f);
        if (muzzle != null)
        {
            muzzle.SetActive(false);
        }
        muzzleRutina = null;
    }
}
