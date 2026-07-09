using UnityEngine;

// Controla armas del jugador, disparo, cambio de arma y recarga.
public class Disparar : MonoBehaviour
{
    [System.Serializable]
    class DatosArma
    {
        // Nombre que se muestra en la interfaz.
        public string nombre;
        // Dano infligido por impacto.
        public int dano;
        // Distancia maxima del raycast.
        public float alcance;
        // Tiempo minimo entre disparos consecutivos.
        public float cadencia;
        // Capacidad del cargador.
        public int balasPorCargador;
        // Duracion de la animacion y espera de recarga.
        public float tiempoRecarga;
        // Tamano visual del arma en la UI.
        public Vector2 escalaUI;
        // Tinte aplicado a la imagen del arma en pantalla.
        public Color colorUI;
    }

    // Camara desde la que sale el raycast de disparo.
    public Camera camara;
    // Sonido que se reproduce al disparar.
    public AudioClip sonidoDisparo;
    // Objeto visual del fogonazo.
    public GameObject muzzle;

    // Conjunto simple de armas disponibles.
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

    // Fuente de audio del jugador.
    private AudioSource fuente;
    // Proximo instante valido para volver a disparar.
    private float proximo = 0f;
    // Balas actuales dentro del cargador activo.
    private int balasActuales;
    // Bloquea disparo mientras se recarga.
    private bool recargando;
    // Arma actualmente seleccionada.
    private int indiceArmaActual;
    // Rutina temporal del fogonazo.
    private Coroutine muzzleRutina;
    // Rutina activa de recarga.
    private Coroutine recargaRutina;

    // Expone la municion actual al HUD.
    public int BalasActuales => balasActuales;

    // Expone la capacidad del arma actual al HUD.
    public int BalasPorCargador => ArmaActual.balasPorCargador;

    // Expone si la recarga esta en proceso.
    public bool EstaRecargando => recargando;

    // Expone el nombre del arma equipada.
    public string NombreArmaActual => ArmaActual.nombre;

    // Acceso rapido al arma actual.
    DatosArma ArmaActual => armas[indiceArmaActual];

    void Start()
    {
        // Prepara audio, municion inicial y HUD al entrar en escena.
        fuente = GetComponent<AudioSource>();
        balasActuales = ArmaActual.balasPorCargador;
        if (muzzle != null) muzzle.SetActive(false);
        JuegoManager.Instance?.ActualizarArma(NombreArmaActual, ArmaActual.escalaUI, ArmaActual.colorUI);
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
    }

    void Update()
    {
        // Si el juego esta pausado o terminado, el jugador no puede disparar ni cambiar arma.
        if (JuegoManager.Instance != null && !JuegoManager.Instance.PuedeControlarJugador)
        {
            return;
        }

        // Permite cambiar entre dos armas simples para variar dano, cadencia y cargador.
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

        // Durante la recarga se bloquea cualquier disparo.
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

            // La cadencia se controla guardando el proximo instante valido de disparo.
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
        // Sonido y fogonazo dan feedback inmediato al jugador.
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

        // El disparo real se resuelve con un raycast desde el centro de la camara.
        Ray ray = camara.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, ArmaActual.alcance))
        {
            Vida v = hit.collider.GetComponentInParent<Vida>();
            if (v != null) v.RecibirDano(ArmaActual.dano);
        }
    }

    void CambiarArma(int nuevoIndice)
    {
        // Ignora cambios invalidos o repetir el arma ya equipada.
        if (nuevoIndice < 0 || nuevoIndice >= armas.Length || nuevoIndice == indiceArmaActual)
        {
            return;
        }

        // Cambiar de arma cancela la recarga anterior y repone su cargador.
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
        // No inicia otra recarga si ya esta llena o el arma ya esta recargando.
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
        // La espera separa la accion de recargar del relleno instantaneo del cargador.
        yield return new WaitForSeconds(ArmaActual.tiempoRecarga);
        balasActuales = ArmaActual.balasPorCargador;
        recargando = false;
        JuegoManager.Instance?.ActualizarMunicion(balasActuales, ArmaActual.balasPorCargador, recargando);
        recargaRutina = null;
    }

    System.Collections.IEnumerator ApagarMuzzle()
    {
        // El fogonazo solo dura un instante corto.
        yield return new WaitForSeconds(0.05f);
        if (muzzle != null)
        {
            muzzle.SetActive(false);
        }
        muzzleRutina = null;
    }
}
