# F.I.C.C.T. Proyecto II Unity SA-INF102

## Entrega

- Informe: [OneDrive](https://1drv.ms/w/c/e18ab7acee2f0bb4/IQCZQeeDM9qcRrfy_GO4UwluAZkeKsUUsL6JR-cRwK6ZgAA?e=WIqzYj)
- Video: [OneDrive](https://1drv.ms/v/c/e18ab7acee2f0bb4/IQBL7zmRF0YKS5OmDF9E_Y7KAagpwkPrrbBxJ9xNRUTwh6k?e=vniAsp)

Proyecto practico basado en un `Shooter 3D estilo Doom` desarrollado en Unity. El juego fue ampliado para incluir sistemas de municion, recarga, enemigos con navegacion `NavMesh`, meta con condicion doble de victoria, interfaz de estado, retroalimentacion de dano y botiquines funcionales.

## Descripcion

El jugador recorre un mapa tipo laberinto en primera persona y debe eliminar a todos los enemigos para poder avanzar. La meta amarilla no permite pasar de nivel si aun quedan enemigos vivos. El juego incluye 3 niveles progresivos con distinta cantidad de enemigos y una distribucion dinamica de la meta y botiquines.

## Requisitos

- Unity `6000.5.2f1`
- Plataforma objetivo: `Windows, Mac, Linux`

## Como abrir y ejecutar

1. Abrir Unity Hub.
2. Agregar o abrir la carpeta [`Shooter3DDoom`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom:1).
3. Verificar que la escena activa sea [`Assets/Scenes/SampleScene.unity`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scenes/SampleScene.unity:1).
4. Presionar `Play` en el editor de Unity.

## Controles

- `W A S D` o flechas: mover al jugador
- `Mouse`: mirar alrededor
- `Click izquierdo`: disparar
- `R`: recargar
- `1` y `2`: cambiar arma
- `Esc`: pausar o reanudar

## Funcionalidades implementadas

- Sistema de municion limitada y recarga con tiempo de espera.
- HUD con municion, vida, arma actual, enemigos restantes y estado del nivel.
- Enemigos con patrulla, deteccion del jugador, persecucion y ataque a distancia.
- Uso de `NavMesh` para navegacion enemiga.
- Condicion doble de victoria:
  primero eliminar a todos los enemigos y despues llegar a la meta.
- 3 niveles con distinta cantidad de enemigos.
- Meta amarilla con posicion variable entre niveles.
- Pantalla de `Game Over` con botones `Reintentar` y `Salir`.
- Pantalla de `Victoria` con opcion de volver a jugar o salir.
- Flash rojo en pantalla cuando el jugador recibe dano.
- Botiquines funcionales con imagen `botiquin.png`, curacion limitada y sonido al recogerlos.

## Estructura principal

- [`Assets/Scripts/JuegoManager.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/JuegoManager.cs:1)
  controla niveles, interfaz, meta, enemigos, botiquines, pausa, derrota y victoria.
- [`Assets/Scripts/Disparar.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Disparar.cs:1)
  maneja disparo, armas, recarga y municion.
- [`Assets/Scripts/EnemigoIA.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/EnemigoIA.cs:1)
  implementa patrulla, persecucion, vision y disparo enemigo.
- [`Assets/Scripts/Vida.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/Vida.cs:1)
  administra vida, dano, curacion y muerte.
- [`Assets/Scripts/RecogerBotiquin.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/RecogerBotiquin.cs:1)
  controla el comportamiento del botiquin.
- [`Assets/Scripts/ActivadorMeta.cs`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scripts/ActivadorMeta.cs:1)
  detecta cuando el jugador entra a la meta.

## Assets relevantes

- Escena principal:
  [`Assets/Scenes/SampleScene.unity`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Scenes/SampleScene.unity:1)
- Sprite del botiquin:
  [`Assets/Resources/Sprites/botiquin.png`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Resources/Sprites/botiquin.png:1)
- Sonidos:
  [`Assets/Sonidos`](/home/jhasmany/Repository/Programacion%20Grafica/F.I.C.C.T.-Proyecto_II-Unity-SA-INF102/Shooter3DDoom/Assets/Sonidos:1)

## Objetivo del juego

Superar los 3 niveles eliminando a todos los enemigos de cada ronda y alcanzando la meta para avanzar.
