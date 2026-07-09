using UnityEngine;

// Controla el movimiento basico en primera persona del jugador.
public class PrimeraPersona : MonoBehaviour
{
    // Velocidad de desplazamiento horizontal.
    public float velocidad = 5f;
    // Sensibilidad del mouse para girar la camara.
    public float sensibilidad = 2f;
    // Fuerza constante usada para simular la gravedad.
    public float gravedad = -9.81f;
    // Referencia a la camara hija que sube y baja con el mouse.
    public Transform camara;

    // CharacterController usado para mover al jugador sin Rigidbody.
    private CharacterController cc;
    // Angulo vertical acumulado para limitar cuanto mira arriba o abajo.
    private float pitch = 0f;
    // Velocidad acumulada del eje Y para aplicar gravedad.
    private Vector3 velY;

    void Start()
    {
        // Obtiene el componente requerido para mover al jugador.
        cc = GetComponent<CharacterController>();
        // Al iniciar, oculta y bloquea el cursor para modo FPS.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Si el juego esta pausado o terminado, el jugador no puede moverse.
        if (JuegoManager.Instance != null && !JuegoManager.Instance.PuedeControlarJugador)
        {
            return;
        }

        // Lee el movimiento del mouse y lo transforma en rotacion horizontal y vertical.
        float mx = Input.GetAxis("Mouse X") * sensibilidad;
        float my = Input.GetAxis("Mouse Y") * sensibilidad;
        transform.Rotate(0, mx, 0);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        camara.localEulerAngles = new Vector3(pitch, 0, 0);

        // Convierte las teclas de movimiento en un vector local relativo al jugador.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 mov = (transform.right * h + transform.forward * v).normalized * velocidad;

        // Si esta en el suelo, mantiene una pequena fuerza hacia abajo para estabilidad.
        if (cc.isGrounded && velY.y < 0) velY.y = -2f;
        // Acumula gravedad para caidas y saltos futuros si se quisieran agregar.
        velY.y += gravedad * Time.deltaTime;

        // Aplica el movimiento horizontal y vertical en una sola llamada.
        cc.Move((mov + velY) * Time.deltaTime);
    }
}
