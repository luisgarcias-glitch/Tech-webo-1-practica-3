using CatalogoGalactico.Models;
using CatalogoGalactico.Services;

namespace CatalogoGalactico.Endpoints;

public static class CartaEndpoints
{
    public static IEndpointRouteBuilder MapCartaEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/cartas").WithTags("Cartas");

        g.MapGet("/", (CartaService s, int? personajeId, int? poderMinimo, int? poderMaximo, int? peligrosidadMinima) =>
                Results.Ok(s.Listar(personajeId, poderMinimo, poderMaximo, peligrosidadMinima)))
            .WithSummary("Lista cartas con filtros opcionales combinables")
            .Produces<List<CardPersonaje>>();

        g.MapGet("/{id:int}", (int id, CartaService s) => Results.Ok(s.Obtener(id)))
            .WithSummary("Obtiene una carta por id")
            .Produces<CardPersonaje>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        g.MapPost("/", (CardRequest r, CartaService s) =>
            {
                var creada = s.Crear(r);
                return Results.Created($"/cartas/{creada.Id}", creada);
            })
            .WithSummary("Crea la carta de un personaje (una por personaje)")
            .WithDescription("poder: 1-100. nivelPeligrosidad: 1-10. El personaje debe existir y no tener carta previa.")
            .Produces<CardPersonaje>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapPut("/{id:int}", (int id, CardRequest r, CartaService s) => Results.Ok(s.Actualizar(id, r)))
            .WithSummary("Actualiza una carta")
            .Produces<CardPersonaje>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
