using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class CartaService(DataStore store)
{
    public List<CardPersonaje> Listar(int? personajeId, int? poderMinimo, int? poderMaximo, int? peligrosidadMinima)
    {
        lock (store.Sync)
        {
            return store.Cartas
                .Where(c => personajeId is null || c.PersonajeId == personajeId)
                .Where(c => poderMinimo is null || c.Poder >= poderMinimo)
                .Where(c => poderMaximo is null || c.Poder <= poderMaximo)
                .Where(c => peligrosidadMinima is null || c.NivelPeligrosidad >= peligrosidadMinima)
                .OrderBy(c => c.Id)
                .ToList();
        }
    }

    public CardPersonaje Obtener(int id)
    {
        lock (store.Sync)
        {
            return store.Cartas.FirstOrDefault(c => c.Id == id)
                   ?? throw new NotFoundException($"No existe una carta con id {id}.");
        }
    }

    public CardPersonaje Crear(CardRequest r)
    {
        Validar(r);
        lock (store.Sync)
        {
            if (!store.Personajes.Any(p => p.Id == r.PersonajeId))
                throw new ReglaNegocioException($"El personaje {r.PersonajeId} no existe.");
            if (store.Cartas.Any(c => c.PersonajeId == r.PersonajeId))
                throw new ReglaNegocioException($"El personaje {r.PersonajeId} ya tiene una carta (relación 1:1).");

            var carta = new CardPersonaje(store.NuevoIdCarta(), r.PersonajeId, r.Poder,
                r.HabilidadEspecial.Trim(), r.Arma.Trim(), r.NivelPeligrosidad, LimpiarUrl(r.ImagenUrl));
            store.Cartas.Add(carta);
            return carta;
        }
    }

    public CardPersonaje Actualizar(int id, CardRequest r)
    {
        Validar(r);
        lock (store.Sync)
        {
            var idx = store.Cartas.FindIndex(c => c.Id == id);
            if (idx < 0) throw new NotFoundException($"No existe una carta con id {id}.");

            var actual = store.Cartas[idx];
            if (r.PersonajeId != actual.PersonajeId)
                throw new ReglaNegocioException("No se puede cambiar el personaje al que pertenece la carta.");

            var actualizada = actual with
            {
                Poder = r.Poder, HabilidadEspecial = r.HabilidadEspecial.Trim(), Arma = r.Arma.Trim(),
                NivelPeligrosidad = r.NivelPeligrosidad, ImagenUrl = LimpiarUrl(r.ImagenUrl)
            };
            store.Cartas[idx] = actualizada;
            return actualizada;
        }
    }

    private static void Validar(CardRequest r)
    {
        if (r is null) throw new ReglaNegocioException("El cuerpo de la solicitud es obligatorio.");
        Validacion.Rango(r.Poder, 1, 100, "poder");
        Validacion.Rango(r.NivelPeligrosidad, 1, 10, "nivelPeligrosidad");
        Validacion.Requerido(r.HabilidadEspecial, "habilidadEspecial");
        Validacion.Requerido(r.Arma, "arma");
        if (!string.IsNullOrWhiteSpace(r.ImagenUrl) &&
            !(Uri.TryCreate(r.ImagenUrl.Trim(), UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            throw new ReglaNegocioException("El campo 'imagenUrl' debe ser una URL http(s) válida.");
    }

    private static string? LimpiarUrl(string? url) => string.IsNullOrWhiteSpace(url) ? null : url.Trim();
}
