# PointAndClickDemo

Prueba técnica para **Mimical Studio**: una aventura gráfica point & click desarrollada en **Unity 6000.0.68f1** con el material proporcionado.

**Controles:** click izquierdo para moverse e interactuar. También hay soporte de mando (stick izquierdo mueve un cursor virtual, botón sur acciona), y un desplegable arriba a la izquierda para cambiar entre los dos modos de cámara.

---

## Uso de la IA

He usado IA para hacerme **code review** del proyecto: repasar el código buscando bugs, nomenclatura y discutir la arquitecturas. También me ha ayudado a redactar esta documentación.


---

## Requisitos

### 1. El personaje se mueve por el escenario haciendo click

`PlayerController` recibe la posición del puntero a través de `PointerService`, que abstrae ratón y mando tras la interfaz `IPointerSource`, y pide una ruta a un `IPathProvider`. `CharacterMovement` se limita a recorrer esa ruta waypoint a waypoint.

La separación en interfaces es deliberada: el jugador no sabe cómo se calculan las rutas ni de dónde vienen los clicks, así que se puede cambiar el algoritmo de pathfinding o añadir soporte táctil sin tocar `PlayerController`.

### 2. El personaje no se desplaza a zonas no transitables

`GridPathfinder` rasteriza un `PolygonCollider2D` a una rejilla de celdas y ejecuta **A\*** sobre ella. Tres detalles que marcan la diferencia entre que funcione y que se vea bien:

- La rejilla se **erosiona** con el radio del agente, de modo que el personaje no camina pegado a las paredes ni se cuela por huecos de un píxel.
- Las diagonales **no cortan esquinas**: para pasar en diagonal, ambas celdas ortogonales deben estar libres.
- Si el click cae en zona bloqueada, se busca la celda transitable más cercana por anillos crecientes en lugar de ignorar la orden, que es lo que espera el jugador.

Al final se aplica *string pulling*: se eliminan los waypoints intermedios con línea de visión directa, para que la ruta no tenga el aspecto escalonado típico de una rejilla.

**Por qué A\* y no una solución de terceros:** en un proyecto real probablemente compensaría tirar de un paquete ya hecho, que llega con optimizaciones, herramientas de debug y años de casos límite resueltos. Aquí he implementado A\* a mano porque es un algoritmo que estudié en la carrera y, tratándose de una prueba técnica, me pareció más interesante enseñar cómo lo resuelvo que integrar una dependencia.

### 3. El personaje se anima al desplazarse

El Animator tiene dos estados, `Idle` y `Walk`, con un parámetro `Speed` y transiciones sin exit time. `CharacterAnimator` lo alimenta desde `CharacterMovement.Velocity` y voltea el sprite con `flipX` según la dirección horizontal.

La velocidad se **normaliza por la escala de profundidad** antes de compararla con el umbral: si no, el mismo movimiento daría valores 20 veces mayores en primer plano que al fondo, y el umbral tendría que ser distinto en cada punto del escenario.

### 4. El personaje se reescala en función de la profundidad

`DepthScaler` interpola con `InverseLerp`/`Lerp` entre dos referencias en Y y dos escalas, todo editable en el inspector. Dibuja gizmos con ambas líneas de referencia para poder ajustarlas a ojo sobre el fondo. En lugar de hacerlo lineal en una futura implementacion podría añadirse una curva.

`CharacterMovement` multiplica su velocidad por esa misma escala, de forma que el paso *aparente* del personaje es constante: al fondo se mueve más despacio en unidades de mundo, pero recorre lo mismo respecto a su propio tamaño.

### 5. La ventana es responsive

`CameraFraming` reduce el encuadre a una única fórmula: se calcula el ajuste por altura y por anchura, y **Contain toma el mayor** (cabe todo, sobra pantalla) mientras **Cover toma el menor** (llena la pantalla, sobra fondo). `CameraFramingController` la reevalúa cada vez que cambia el tamaño de la ventana, no cada frame.

El fondo proporcionado es de 72 × 21.6 unidades, es decir **3.33:1**, mucho más panorámico que cualquier monitor. Con la estrategia *contain* a 16:9 casi la mitad de la pantalla queda en barras negras, así que hay dos modos seleccionables desde el HUD:

- **Contain**: se ve la escena entera de un vistazo, con barras. La cámara no se mueve.
- **Cover**: la cámara llena la pantalla y sigue al jugador con **Cinemachine** (`CinemachineCamera` + `PositionComposer` + `Confiner2D`), confinada a los límites del fondo. Sin barras de 4:3 a 21:9.

Aquí sí he tirado de Cinemachine, al contrario que con el pathfinding: es un paquete con el que ya estoy acostumbrado a trabajar y es extremadamente configurable, así que el seguimiento, el suavizado, la zona muerta y el confinamiento a los bordes del fondo salen de ajustar componentes en vez de escribirlos.

También se ajustaron los Player Settings: ventana redimensionable, modo windowed y *reset resolution on window resize*, para que el requisito sea verificable en build y no solo en el editor.

---

## Añadidos

Más allá de los requisitos hay dos cosas: soporte de mando y un sistema de interactuables.

### Soporte de mando además de ratón

El juego acepta indistintamente **ratón y mando**. `PointerService` conmuta entre ambos según el esquema de control activo del Input System, y expone siempre la misma interfaz `IPointerSource`: posición en pantalla, posición en mundo y si se ha pulsado.

Con mando, `GamepadVirtualCursor` mueve un cursor virtual con el stick izquierdo y acciona con el botón sur. Como todo lo que consume el puntero habla con la interfaz y no con el ratón, añadir esta segunda fuente no obligó a tocar ni `PlayerController` ni el cursor en pantalla.

He implementado esto por que me parece importante desarrollar el soporte de mando desde un principio en cualquier juego ya que facilita muchisimo cualquier port para un futuro.

### Interactables

Hay un pequeño sistema de **interactables** para que la escena tenga un objetivo: recoger una llave y abrir una puerta.

Todo cuelga de la interfaz `IInteractable`, con dos métodos: `CanBeInteracted` y `Interact`. `PlayerController` no conoce ningún tipo concreto, solo la interfaz, así que añadir un interactuable nuevo no le afecta. `Collectable` y `Door` la implementan, y `Door` además comprueba el `Inventory` del jugador antes de dejarse abrir.

Si un objeto está fuera de alcance, el click no se descarta: el personaje camina hacia él. Es el comportamiento que espera cualquiera que haya jugado a una aventura gráfica.

**Cómo se extendería el inventario:** ahora mismo `Inventory` es deliberadamente mínimo, un `bool CanUseDoor`, porque es todo lo que la escena necesita. El paso natural sería sustituirlo por **ScriptableObjects**: un asset `ItemDefinition` por objeto (id, nombre, icono, descripción) y un `HashSet<ItemDefinition>` en el inventario, con `Has(item)` y `Add(item)`. `Collectable` llevaría un campo con el ítem que otorga y `Door` con el que requiere, ambos asignables desde el inspector.

Eso permitiría crear objetos y puertas nuevos sin escribir una línea de código, y desacoplaría los interactuables entre sí, que es el problema real del diseño actual: `Collectable` sabe hoy que existe una puerta.

---

## Estructura

```
Assets/Scripts/
├── Cameras/        encuadre de cámara y sus dos estrategias
├── Characters/     movimiento, animación y escalado por profundidad
│   └── Player/     control del jugador e inventario
├── Input/          abstracción de puntero: ratón y cursor de mando
├── Interactables/  IInteractable, recogibles y puerta
├── Pathfinding/    A* sobre rejilla e IPathProvider
└── UI/             HUD de selección de modo de cámara
```

Cada carpeta se corresponde con un namespace bajo `PointAndClickDemo`, y el código sigue las convenciones de nomenclatura recomendadas por Unity.
