using CatalogoGalactico.Models;
using CatalogoGalactico.Services;

namespace CatalogoGalactico.Endpoints;

public static class HabilidadEndpoints
{
    public static IEndpointRouteBuilder MapHabilidadEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- Catálogo de habilidades ----
        var g = app.MapGroup("/habilidades").WithTags("Habilidades");

        g.MapGet("/", (HabilidadService s, string? tipo, string? nombre, int? bonusMinimo) =>
                Results.Ok(s.Listar(tipo, nombre, bonusMinimo)))
            .WithSummary("Lista el catálogo de habilidades con filtros opcionales")
            .WithDescription("Filtros: tipo (Fuerza|Combate|Tecnica|Liderazgo|Pilotaje), nombre y bonusMinimo.")
            .Produces<List<Habilidad>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapGet("/{id:int}", (int id, HabilidadService s) => Results.Ok(s.Obtener(id)))
            .WithSummary("Obtiene una habilidad por id")
            .Produces<Habilidad>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapGet("/{id:int}/personajes", (int id, HabilidadService s) => Results.Ok(s.ListarPersonajes(id)))
            .WithSummary("Personajes que tienen una habilidad, con su nivel")
            .Produces<List<PersonajeConHabilidad>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapPost("/", (HabilidadRequest r, HabilidadService s) =>
            {
                var creada = s.Crear(r);
                return Results.Created($"/habilidades/{creada.Id}", creada);
            })
            .WithSummary("Crea una habilidad en el catálogo")
            .WithDescription("bonus: 1-20. El nombre debe ser único.")
            .Produces<Habilidad>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapPut("/{id:int}", (int id, HabilidadRequest r, HabilidadService s) => Results.Ok(s.Actualizar(id, r)))
            .WithSummary("Actualiza una habilidad")
            .Produces<Habilidad>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapDelete("/{id:int}", (int id, HabilidadService s) =>
            {
                s.Eliminar(id);
                return Results.NoContent();
            })
            .WithSummary("Elimina una habilidad que ningún personaje usa")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Habilidades anidadas en personajes (tabla intermedia) ----
        var p = app.MapGroup("/personajes/{id:int}/habilidades").WithTags("Habilidades de personajes");

        p.MapGet("/", (int id, HabilidadService s) => Results.Ok(s.ListarPorPersonaje(id)))
            .WithSummary("Habilidades de un personaje con su nivel")
            .Produces<List<HabilidadAsignada>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        p.MapPost("/", (int id, AsignarHabilidadRequest r, HabilidadService s) =>
            {
                var asignada = s.Asignar(id, r);
                return Results.Created($"/personajes/{id}/habilidades", asignada);
            })
            .WithSummary("Asigna una habilidad a un personaje")
            .WithDescription("nivel: 1-5. No se permiten duplicados. Las habilidades de tipo Fuerza solo " +
                             "pueden asignarse a personajes con fuerzaSensitivo = true.")
            .Produces<HabilidadAsignada>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        p.MapPut("/{habilidadId:int}", (int id, int habilidadId, ActualizarNivelRequest r, HabilidadService s) =>
                Results.Ok(s.ActualizarNivel(id, habilidadId, r.Nivel)))
            .WithSummary("Cambia el nivel de una habilidad del personaje")
            .Produces<HabilidadAsignada>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        p.MapDelete("/{habilidadId:int}", (int id, int habilidadId, HabilidadService s) =>
            {
                s.Quitar(id, habilidadId);
                return Results.NoContent();
            })
            .WithSummary("Quita una habilidad a un personaje")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
