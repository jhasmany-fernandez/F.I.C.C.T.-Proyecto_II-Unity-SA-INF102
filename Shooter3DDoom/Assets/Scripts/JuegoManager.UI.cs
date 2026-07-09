using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Agrupa toda la construccion de interfaz, paneles y efectos visuales del juego.
public partial class JuegoManager
{
    void ConfigurarUI()
    {
        // Recupera el canvas de la escena y construye el HUD por codigo.
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
        // Detiene cualquier animacion visual pendiente al recargar escena.
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
        // Borra referencias invalidadas por el cambio o recarga de escena.
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
        // Crea un texto simple reutilizable para los elementos del HUD.
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
        // Construye la mira fija del centro de pantalla.
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
        // Acomoda la imagen del arma existente en el canvas para animarla luego.
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
        // Overlay a pantalla completa usado para feedback de dano y curacion.
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
        // Panel reutilizable para derrota, victoria, pausa y confirmacion de salida.
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
        // Helper para crear textos hijos de paneles y botones.
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
        // Boton principal del panel, normalmente usado para reintentar o continuar.
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
        // Boton secundario usado para salir, cancelar o acciones alternativas.
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
        // Usa una fuente integrada de Unity para no depender de assets extra.
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void CambiarCursor(bool visible)
    {
        // Muestra u oculta cursor y tambien la mira segun el contexto del juego.
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        if (textoMira != null)
        {
            textoMira.gameObject.SetActive(!visible);
        }
    }

    void MostrarPanel(string titulo, string textoBoton)
    {
        // Activa el panel y resetea su configuracion basica antes de personalizarlo.
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

    void ActualizarNivel()
    {
        // Refresca el texto superior con nivel actual y tiempo restante.
        if (textoCuentaRegresiva == null)
        {
            return;
        }

        textoCuentaRegresiva.text = $"Nivel: {indiceNivelActual + 1} | Cuenta atras: {Mathf.CeilToInt(TiempoRestanteCaceria)}";
    }

    public void ActualizarMunicion(int actual, int maximo, bool recargando)
    {
        // Muestra municion actual y agrega estado de recarga cuando corresponde.
        if (textoMunicion == null)
        {
            return;
        }

        textoMunicion.text = recargando ? $"Municion: {actual}/{maximo} (Recargando...)" : $"Municion: {actual}/{maximo}";
    }

    public void ActualizarVida(int actual, int maximo)
    {
        // Refresca el texto de vida del jugador.
        if (textoVida == null)
        {
            return;
        }

        textoVida.text = $"Vida: {actual}/{maximo}";
    }

    public void ActualizarArma(string nombre, Vector2 tamanoUI, Color color)
    {
        // Sincroniza nombre, tamano y color del arma mostrada en pantalla.
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
        // Disparo sacude el arma brevemente para dar impacto visual.
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
        // Recarga mueve el arma hacia abajo y la devuelve al terminar.
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
        // Muestra cuantos enemigos siguen vivos en el nivel.
        if (textoEnemigos == null)
        {
            return;
        }

        textoEnemigos.text = $"Enemigos: {enemigos.Count}";
    }

    public void MostrarFlashDano()
    {
        // Inicia el overlay rojo de dano recibido.
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
        // Inicia el overlay verde de curacion recibida.
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
        // Desvanece rapidamente el overlay rojo hasta volver a transparente.
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
        // Desvanece rapidamente el overlay verde de curacion.
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
        // Escribe mensajes temporales o persistentes en la barra de estado.
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
        // Reinicia la rutina de cuenta atras del comienzo del nivel.
        if (rutinaCuentaRegresiva != null)
        {
            StopCoroutine(rutinaCuentaRegresiva);
        }

        rutinaCuentaRegresiva = StartCoroutine(RutinaCuentaRegresiva());
    }

    IEnumerator LimpiarEstado(float duracion)
    {
        // Pasado un tiempo, vuelve al mensaje contextual del estado del nivel.
        yield return new WaitForSecondsRealtime(duracion);
        if (estado == EstadoJuego.Jugando)
        {
            textoEstado.text = EnemigosPuedenCazarJugador ? "Ahora ellos te estan cazando" : "Tienes 5 segundos para cazarlos primero";
        }
        rutinaEstado = null;
    }

    IEnumerator RutinaCuentaRegresiva()
    {
        // Actualiza el texto hasta que los enemigos entren en modo caceria.
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
        // Desplaza y rota el arma en dos fases para simular retroceso.
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
        // Baja el arma y luego la regresa durante el tiempo de recarga.
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
}
