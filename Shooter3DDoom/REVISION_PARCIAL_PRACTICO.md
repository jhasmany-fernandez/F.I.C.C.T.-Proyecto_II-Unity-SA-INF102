# Revision Del Parcial Practico

Proyecto revisado: `Shooter3DDoom`

Objetivo de esta revision: comprobar, punto por punto, si el proyecto cumple con los requisitos obligatorios y el punto opcional indicado.

## Resultado General

- `1. Sistema de municion y recarga`: `Cumple`
- `2. Enemigos que persiguen y disparan`: `Cumple`
- `3. Contador de enemigos + doble condicion de victoria`: `Cumple`
- `4. Menu de Game Over con reinicio y cursor libre`: `Cumple`
- `5. Feedback de dano con parpadeo rojo`: `Cumple`
- `Opcional 1. Botiquin funcional`: `Cumple`

## 1. Sistema de municion y recarga

Veredicto: `Cumple`

Evidencia encontrada:

- En [Disparar.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:65>) se guardan las balas actuales del cargador.
- En [Disparar.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:134>) se recarga con la tecla `R`.
- En [Disparar.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:147>) y [Disparar.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:160>) se detecta cuando el cargador llega a cero y se inicia la recarga.
- En [Disparar.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:247>) la recarga espera un tiempo antes de rellenar el cargador.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:333>) se muestra la municion en pantalla.

Conclusion:

El arma tiene municion limitada, la recarga no es instantanea, puede activarse con `R` y el HUD muestra el estado actual de municion y recarga.

## 2. Enemigos que persiguen y disparan

Veredicto: `Cumple`

Evidencia encontrada:

- En [JuegoManager.Nivel.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Nivel.cs:344>) se crean enemigos por codigo para cada nivel.
- En [EnemigoIA.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/EnemigoIA.cs:138>) el enemigo usa `NavMeshAgent` para ir hacia la posicion del jugador.
- En [EnemigoIA.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/EnemigoIA.cs:157>) el enemigo dispara cuando esta a rango, tiene cadencia disponible y posee linea de vision.
- En [EnemigoIA.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/EnemigoIA.cs:267>) el ataque usa raycast y valida impacto real antes de aplicar dano.
- En [EnemigoIA.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/EnemigoIA.cs:305>) el raycast usa distancia exacta al jugador e ignora triggers para evitar disparos atravesando paredes.

Conclusion:

Los enemigos se mueven usando navegacion `NavMesh`, persiguen al jugador y atacan con un sistema de disparo tipo raycast equivalente al del jugador.

## 3. Contador de enemigos + condicion doble de victoria

Veredicto: `Cumple`

Evidencia encontrada:

- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:409>) se actualiza el texto `Enemigos: X`.
- En [JuegoManager.Nivel.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Nivel.cs:476>) al morir un enemigo se elimina de la lista y se refresca el contador.
- En [ActivadorMeta.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/ActivadorMeta.cs:6>) la meta llama al gestor cuando el jugador entra al trigger.
- En [JuegoManager.Flujo.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Flujo.cs:50>) la meta solo permite completar el nivel si `enemigos.Count == 0`.

Conclusion:

La victoria no depende solo de tocar la meta. Tambien exige haber eliminado a todos los enemigos vivos del nivel.

## 4. Menu de Game Over con reinicio

Veredicto: `Cumple`

Evidencia encontrada:

- En [Vida.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Vida.cs:117>) cuando muere el jugador se llama a `JuegoManager.JugadorMurio()`.
- En [JuegoManager.Flujo.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Flujo.cs:15>) se activa el estado de `Game Over`.
- En [JuegoManager.Flujo.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Flujo.cs:26>) se pausa el juego con `Time.timeScale = 0f`.
- En [JuegoManager.Flujo.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Flujo.cs:27>) se libera el cursor.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:186>) se crea el panel final reutilizable.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:231>) se crea el boton principal `Reintentar`.

Conclusion:

Al morir no se recarga directamente la escena. Se muestra una pantalla de Game Over con opcion de reintento y el cursor queda libre. Ademas, ahora existe un boton extra de `Salir`, aunque ese agregado no era obligatorio.

## 5. Animacion o feedback de dano

Veredicto: `Cumple`

Evidencia encontrada:

- En [Vida.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Vida.cs:75>) cuando el jugador recibe dano se llama a `MostrarFlashDano()`.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:167>) se crea un `overlay` de pantalla completa.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:420>) se inicia el efecto de dano.
- En [JuegoManager.Interfaz.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Interfaz.cs:452>) el overlay se vuelve rojo y luego se desvanece.

Conclusion:

El proyecto si tiene el parpadeo rojo breve en pantalla al recibir dano, que es exactamente lo pedido por el requisito.

## Opcional 1. Botiquin funcional

Veredicto: `Cumple`

Evidencia encontrada:

- En [JuegoManager.Nivel.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Nivel.cs:405>) se crea el botiquin.
- En [JuegoManager.Nivel.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Nivel.cs:415>) se carga la textura `Resources/Sprites/botiquin`.
- En [JuegoManager.Nivel.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.Nivel.cs:429>) esa textura se convierte en `Sprite` y se muestra con `SpriteRenderer`.
- En [RecogerBotiquin.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/RecogerBotiquin.cs:71>) el botiquin detecta al jugador al tocarlo.
- En [RecogerBotiquin.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/RecogerBotiquin.cs:88>) si el jugador ya esta al maximo no consume el botiquin.
- En [Vida.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Vida.cs:97>) la curacion respeta el tope maximo de vida usando `Mathf.Min`.
- En [RecogerBotiquin.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/RecogerBotiquin.cs:95>) reproduce sonido al recogerlo.
- En [RecogerBotiquin.cs](</home/jhasmany/Repository/Programacion Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/RecogerBotiquin.cs:102>) el botiquin desaparece.

Conclusion:

El botiquin usa el recurso visual pedido, cura al jugador sin superar su vida maxima, se consume solo cuando realmente recupera vida y desaparece con sonido.

## Observaciones Finales

- A nivel de codigo, los requisitos solicitados si estan implementados.
- La revision hecha aqui fue estatica sobre scripts y flujo del proyecto.
- La ultima validacion recomendable es abrir Unity, dejar que recompilen los scripts y probar una partida completa para confirmar que todo se comporta igual en ejecucion.



// 1. AZUL — Configuración {#2563EB, 10}

// 2. VERDE — Funciones principales {#16A34A, 10}

// 3. NARANJA — Validaciones {#D97706, 10}

// 4. ROJO — Errores y excepciones {#DC2626, 10}

// 5. VIOLETA — Servicios externos {#7C3AED, 10}

// 6. CELESTE — Base de datos {#0891B2, 10}