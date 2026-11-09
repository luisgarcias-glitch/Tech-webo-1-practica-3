using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class PersonajeService(DataStore store)
{
    public List<Personaje> Listar(string? faccion, string? estado, string? especie,
        string? afiliacion, bool? fuerzaSensitivo, string? nombre)
    {
        var f = Validacion.ParseEnumOpcional<Faccion>(faccion, "faccion");
        var e = Validacion.ParseEnumOpcional<EstadoPersonaje>(estado, "estado");

        lock (store.Sync)
        {
            return store.Personajes
                .Where(p => f is null || p.Faccion == f)
                .Where(p => e is null || p.Estado == e)
                .Where(p => fuerzaSensitivo is null || p.FuerzaSensitivo == fuerzaSensitivo)
                .Where(p => string.IsNullOrWhiteSpace(especie) || p.Especie.Contains(especie.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(p => string.IsNullOrWhiteSpace(afiliacion) || p.Afiliacion.Contains(afiliacion.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(p => string.IsNullOrWhiteSpace(nombre) || p.Nombre.Contains(nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Id)
                .ToList();
        }
    }

    public Personaje Obtener(int id)
    {
        lock (store.Sync)
        {
            return store.Personajes.FirstOrDefault(p => p.Id == id)
                   ?? throw new NotFoundException($"No existe un personaje con id {id}.");
        }
    }

    /// <summary>GET /personajes/{id}/con-card: combina Personajes + CardsPersonaje.</summary>
    public PersonajeConCardResponse ObtenerConCard(int id)
    {
        lock (store.Sync)
        {
            var p = store.Personajes.FirstOrDefault(x => x.Id == id)
                    ?? throw new NotFoundException($"No existe un personaje con id {id}.");
            var c = store.Cartas.FirstOrDefault(x => x.PersonajeId == id)
                    ?? throw new NotFoundException($"{p.Nombre} todavía no tiene carta.");

            return new PersonajeConCardResponse(p.Id, p.Nombre, p.Especie, p.Faccion, p.Afiliacion,
                p.Estado, p.FuerzaSensitivo, p.Image, c.Poder, c.HabilidadEspecial, c.Arma,
                c.NivelPeligrosidad, c, HabilidadService.ObtenerAsignadas(store, id));
        }
    }

    public Personaje Crear(PersonajeRequest r)
    {
        var (nombre, especie, faccion, afiliacion) = Validar(r);
        lock (store.Sync)
        {
            ValidarNombreUnico(nombre, null);
            var p = new Personaje(store.NuevoIdPersonaje(), nombre, especie, faccion, afiliacion,
                r.Estado ?? EstadoPersonaje.Vivo, r.FuerzaSensitivo, LimpiarImagen(r.Image));
            store.Personajes.Add(p);
            return p;
        }
    }

    public Personaje Actualizar(int id, PersonajeRequest r)
    {
        var (nombre, especie, faccion, afiliacion) = Validar(r);
        lock (store.Sync)
        {
            var idx = store.Personajes.FindIndex(p => p.Id == id);
            if (idx < 0) throw new NotFoundException($"No existe un personaje con id {id}.");

            var actual = store.Personajes[idx];
            ValidarNombreUnico(nombre, id);

            var nuevoEstado = r.Estado ?? actual.Estado;
            if (nuevoEstado != EstadoPersonaje.Muerto && TieneMuerteConsignada(id))
                throw new ReglaNegocioException(
                    $"{actual.Nombre} tiene su muerte consignada en un evento; no se puede cambiar su estado a '{nuevoEstado}'. " +
                    "Modifique primero el evento correspondiente.");

            // No se puede quitar la sensibilidad a la Fuerza si ya tiene habilidades de tipo Fuerza.
            if (!r.FuerzaSensitivo && store.PersonajeHabilidades.Any(a => a.PersonajeId == id &&
                    store.Habilidades.Any(h => h.Id == a.HabilidadId && h.Tipo == TipoHabilidad.Fuerza)))
                throw new ReglaNegocioException(
                    $"{actual.Nombre} tiene habilidades de tipo Fuerza; quíteselas antes de marcarlo como no sensible a la Fuerza.");

            var actualizado = actual with
            {
                Nombre = nombre, Especie = especie, Faccion = faccion, Afiliacion = afiliacion,
                Estado = nuevoEstado, FuerzaSensitivo = r.FuerzaSensitivo, Image = LimpiarImagen(r.Image)
            };
            store.Personajes[idx] = actualizado;
            return actualizado;
        }
    }

    public void Eliminar(int id)
    {
        lock (store.Sync)
        {
            var p = store.Personajes.FirstOrDefault(x => x.Id == id)
                    ?? throw new NotFoundException($"No existe un personaje con id {id}.");

            var evento = store.Eventos.FirstOrDefault(e => e.Participantes.Contains(id));
            if (evento is not null)
                throw new ReglaNegocioException(
                    $"No se puede eliminar a {p.Nombre}: participa en el evento '{evento.Nombre}'. " +
                    "Quítelo de los eventos antes de eliminarlo.");

            store.PersonajeHabilidades.RemoveAll(a => a.PersonajeId == id); // asignaciones N:N
            store.Cartas.RemoveAll(c => c.PersonajeId == id); // la carta 1:1 se elimina junto al personaje
            store.Personajes.Remove(p);
        }
    }

    /// <summary>Cambia el estado de un personaje (usado por la lógica de eventos).</summary>
    public void CambiarEstado(int personajeId, EstadoPersonaje estado)
    {
        lock (store.Sync)
        {
            var idx = store.Personajes.FindIndex(p => p.Id == personajeId);
            if (idx < 0) throw new NotFoundException($"No existe un personaje con id {personajeId}.");
            // Con records se crea una copia actualizada y se reemplaza en la lista.
            store.Personajes[idx] = store.Personajes[idx] with { Estado = estado };
        }
    }

    private bool TieneMuerteConsignada(int personajeId) =>
        store.Eventos.Any(e => e.PersonajesMuertos.Contains(personajeId));

    private void ValidarNombreUnico(string nombre, int? idActual)
    {
        if (store.Personajes.Any(p => p.Id != idActual && p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
            throw new ReglaNegocioException($"Ya existe un personaje llamado '{nombre}'.");
    }

    private static string? LimpiarImagen(string? url) => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private static (string nombre, string especie, Faccion faccion, string afiliacion) Validar(PersonajeRequest r)
    {
        if (r is null) throw new ReglaNegocioException("El cuerpo de la solicitud es obligatorio.");
        var nombre = Validacion.Requerido(r.Nombre, "nombre");
        var especie = Validacion.Requerido(r.Especie, "especie");
        var afiliacion = Validacion.Requerido(r.Afiliacion, "afiliacion");
        if (r.Faccion is null)
            throw new ReglaNegocioException("El campo 'faccion' es obligatorio (Rebelde, Imperio o Neutral).");
        if (!string.IsNullOrWhiteSpace(r.Image) &&
            !(Uri.TryCreate(r.Image.Trim(), UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            throw new ReglaNegocioException("El campo 'image' debe ser una URL http(s) válida.");
        return (nombre, especie, r.Faccion.Value, afiliacion);
    }
}
