using CatalogoGalactico.Models;
using CatalogoGalactico.Services;

namespace CatalogoGalactico.Endpoints;

public static class EventoEndpoints
{
    public static IEndpointRouteBuilder MapEventoEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/eventos").WithTags("Eventos");

        g.MapGet("/", (EventoService s, string? nombre, string? ubicacion, int? personajeId, string? desde, string? hasta) =>
                Results.Ok(s.Listar(nombre, ubicacion, personajeId, desde, hasta)))
            .WithSummary("Lista eventos en orden cronológico con filtros opcionales")
            .WithDescription("Filtros: nombre, ubicacion, personajeId, desde y hasta (ej.: desde=10 BBY&hasta=3 ABY).")
            .Produces<List<EventoResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapGet("/{id:int}", (int id, EventoService s) => Results.Ok(s.Obtener(id)))
            .WithSummary("Obtiene un evento por id")
            .Produces<EventoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapPost("/", (EventoRequest r, EventoService s) =>
            {
                var creado = s.Crear(r);
                return Results.Created($"/eventos/{creado.Id}", creado);
            })
            .WithSummary("Crea un evento")
            .WithDescription("La fecha admite '10 BBY', '3 ABY', 'Batalla de Yavin' o un entero. " +
                             "Los personajes listados en personajesMuertos pasan automáticamente a estado Muerto. " +
                             "Se rechaza la participación de un personaje en eventos posteriores a su muerte.")
            .Produces<EventoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapPut("/{id:int}", (int id, EventoRequest r, EventoService s) => Results.Ok(s.Actualizar(id, r)))
            .WithSummary("Actualiza un evento (revalida coherencia temporal y sincroniza estados)")
            .Produces<EventoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapGet("/{id:int}/mvp", (int id, EstadisticasService s) => Results.Ok(s.Mvp(id)))
            .WithSummary("Participante con mayor poder dentro del evento")
            .Produces<MvpResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapPost("/{id:int}/simular", (int id, SimulacionService s, int? semilla) => Results.Ok(s.Simular(id, semilla)))
            .WithSummary("Simula la batalla del evento")
            .WithDescription("Suma el poder de las cartas por bando, aplica un factor aleatorio acotado (0.90-1.10) " +
                             "y guarda resultado y ganador en el evento. Con el parámetro opcional 'semilla' el " +
                             "resultado es reproducible. Si faltan participantes o cartas devuelve 400 sin guardar nada.")
            .Produces<SimulacionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
