# Catálogo galáctico de personajes y eventos

API REST con **Minimal API (.NET 8)** y **Swagger (Swashbuckle)**. Administra personajes, cartas coleccionables y
eventos con almacenamiento en memoria (tres listas). Al reiniciar la aplicación los datos vuelven al estado inicial.

## Ejecutar

```bash
dotnet restore
dotnet run
```

Abre `http://localhost:5100/swagger` (la raíz `/` redirige a Swagger).
Si tu SDK no es .NET 8, cambia `<TargetFramework>` en el `.csproj` (por ejemplo `net9.0`).

## Estructura

| Carpeta / archivo | Contenido |
|---|---|
| `Models/` | Records `Personaje`, `CardPersonaje`, `Evento`; DTOs de solicitud/respuesta; excepciones |
| `Data/` | `DataStore`: tres listas en memoria + datos iniciales |
| `Services/` | Validación, `FechaGalactica`, reglas de eventos, simulación, ranking y MVP |
| `Endpoints/` | Métodos de extensión por recurso, manejador de errores y ejemplos de Swagger |
| `Program.cs` | Registro de servicios, Swagger y mapeo de endpoints |

## Modelo de datos

- **Personaje**: id, nombre, especie, faccion (`Rebelde|Imperio|Neutral`), afiliacion, estado (`Vivo|Muerto|Desconocido`), fuerzaSensitivo.
- **CardPersonaje**: id, personajeId, poder (1-100), habilidadEspecial, arma, nivelPeligrosidad (1-10), imagenUrl. Relación **1:1** con Personaje.
- **Evento**: id, nombre, fecha (año guardado como entero), ubicacion, descripcion, participantes (IDs), personajesMuertos (IDs), resultado, ganador. Relación **N:N** con Personaje.

Convención de fechas: `10 BBY → -10`, `5 BBY → -5`, `Batalla de Yavin → 0`, `3 ABY → 3`, `10 ABY → 10`.
La API acepta `"10 BBY"`, `"3 ABY"`, `"Batalla de Yavin"` o un entero, y responde con el texto y el año guardado (`anioGuardado`).

## Endpoints

| Recurso | Rutas |
|---|---|
| Personajes | `GET /personajes`, `GET /personajes/{id}`, `POST`, `PUT /personajes/{id}`, `DELETE /personajes/{id}` |
| Cartas | `GET /cartas`, `GET /cartas/{id}`, `POST /cartas`, `PUT /cartas/{id}` |
| Eventos | `GET /eventos`, `GET /eventos/{id}`, `POST /eventos`, `PUT /eventos/{id}` |
| Relaciones y acciones | `GET /personajes/{id}/eventos`, `GET /personajes/ranking?por=poder`, `GET /eventos/{id}/mvp`, `POST /eventos/{id}/simular` |

**Filtros**
- `/personajes`: `faccion`, `estado`, `especie`, `afiliacion`, `nombre`, `fuerzaSensitivo` → `/personajes?faccion=Imperio&fuerzaSensitivo=true`
- `/cartas`: `personajeId`, `poderMinimo`, `poderMaximo`, `peligrosidadMinima`
- `/eventos`: `nombre`, `ubicacion`, `personajeId`, `desde`, `hasta` → `/eventos?desde=19 BBY&hasta=3 ABY`
- `/personajes/ranking`: `por=poder|peligrosidad`, `orden=desc|asc`

**Códigos HTTP**: `200` OK, `201` al crear, `204` al eliminar, `400` datos o reglas inválidas, `404` recurso inexistente.
Los errores usan el formato `ProblemDetails` con un mensaje claro.

## Reglas de negocio

1. **Muerte en un evento**: los IDs en `personajesMuertos` deben ser participantes; el estado del personaje pasa a `Muerto` automáticamente (copia del record con `with`, reemplazada en la lista). Si al editar un evento se retira una muerte, el personaje vuelve a `Vivo`.
2. **Consistencia temporal**: un personaje solo puede morir una vez y no puede participar en un evento **posterior** (año mayor) a su muerte consignada. Se valida al crear y al modificar eventos (incluido un cambio de fecha). Un evento del mismo año que la muerte no se considera posterior.
3. **Coherencia entre listas**: no se puede eliminar un personaje que participa en eventos; al eliminarlo se borra su carta. No se puede cambiar a `Vivo` a un personaje con muerte consignada. Un personaje solo puede tener una carta.
4. **Simulación** (`POST /eventos/{id}/simular?semilla=123`): fuerza base = suma del poder de las cartas por bando (Rebelde/Imperio; los Neutrales no combaten). Cada bando recibe un factor aleatorio entre **0.90 y 1.10**. Gana la mayor fuerza final. La respuesta incluye totales, factores, participantes, criterio y ganador. Con la misma `semilla` el resultado se repite. Si hay menos de 2 participantes, faltan cartas o falta un bando, responde `400` **sin guardar nada**; si es exitosa, guarda `resultado` y `ganador` en el evento.
5. **MVP**: participante con mayor poder de carta (desempate por peligrosidad, luego por id).

## Pruebas rápidas (curl)

```bash
# Filtros combinados
curl "http://localhost:5100/personajes?faccion=Imperio&fuerzaSensitivo=true"

# Eventos de un personaje, ranking y MVP
curl http://localhost:5100/personajes/1/eventos
curl "http://localhost:5100/personajes/ranking?por=poder"
curl http://localhost:5100/eventos/6/mvp

# Simulación reproducible
curl -X POST "http://localhost:5100/eventos/6/simular?semilla=42"

# Regla temporal: Obi-Wan murió en 0 (Batalla de Yavin) → participar en 3 ABY da 400
curl -X POST http://localhost:5100/eventos -H "Content-Type: application/json" \
  -d '{"nombre":"Prueba","fecha":"3 ABY","ubicacion":"Hoth","participantes":[5,1]}'

# Muerte automática: crear evento donde muere Lando (id 11) → su estado pasa a Muerto
curl -X POST http://localhost:5100/eventos -H "Content-Type: application/json" \
  -d '{"nombre":"Emboscada","fecha":"5 ABY","ubicacion":"Bespin","participantes":[11,1],"personajesMuertos":[11]}'
curl http://localhost:5100/personajes/11
```

## Datos iniciales

11 personajes (IDs 1-11), 11 cartas y 6 eventos: Duelo en Mustafar (-19), Duelo en la Estrella de la Muerte (0),
Batalla de Yavin (0), Batalla de Hoth (3), Muerte de Yoda (4) y Batalla de Endor (4).

## Defensa: cambios añadidos

- `Personaje.Image`: URL de la imagen del personaje (visible en `GET /personajes`).
- `GET /personajes/{id}/con-card`: ficha de combate (personaje + carta).
- CORS habilitado para `http://localhost:5173` (frontend React/Vite).

## Habilidades (tabla adicional)

Catálogo independiente `Habilidades` + tabla intermedia `PersonajeHabilidades` (relación **N:N** con nivel 1-5).

| Ruta | Descripción |
|---|---|
| `GET/POST/PUT/DELETE /habilidades` | CRUD del catálogo (filtros `tipo`, `nombre`, `bonusMinimo`) |
| `GET /habilidades/{id}/personajes` | Quiénes tienen la habilidad |
| `GET /personajes/{id}/habilidades` | Habilidades de un personaje |
| `POST /personajes/{id}/habilidades` | Asignar (`habilidadId`, `nivel`) |
| `PUT /personajes/{id}/habilidades/{habilidadId}` | Cambiar nivel |
| `DELETE /personajes/{id}/habilidades/{habilidadId}` | Quitar |

Reglas: sin duplicados; las habilidades de tipo `Fuerza` solo para `fuerzaSensitivo = true`; no se elimina una habilidad en uso;
al eliminar un personaje se borran sus asignaciones; `GET /personajes/{id}/con-card` incluye la lista `habilidades`.
