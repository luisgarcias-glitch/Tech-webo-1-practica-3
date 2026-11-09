using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class HabilidadService(DataStore store)
{
    public const int NivelMinimo = 1, NivelMaximo = 5;
    public const int BonusMinimo = 1, BonusMaximo = 20;

    /// <summary>Habilidades de un personaje combinando la tabla intermedia con el catálogo.</summary>
    public static List<HabilidadAsignada> ObtenerAsignadas(DataStore store, int personajeId)
    {
        lock (store.Sync)
        {
            return store.PersonajeHabilidades
                .Where(a => a.PersonajeId == personajeId)
                .Join(store.Habilidades, a => a.HabilidadId, h => h.Id,
                    (a, h) => new HabilidadAsignada(h.Id, h.Nombre, h.Tipo, h.Descripcion, h.Bonus, a.Nivel))
                .OrderBy(x => x.HabilidadId)
                .ToList();
        }
    }

    // ---------------- Catálogo de habilidades ----------------

    public List<Habilidad> Listar(string? tipo, string? nombre, int? bonusMinimo)
    {
        var t = Validacion.ParseEnumOpcional<TipoHabilidad>(tipo, "tipo");
        lock (store.Sync)
        {
            return store.Habilidades
                .Where(h => t is null || h.Tipo == t)
                .Where(h => bonusMinimo is null || h.Bonus >= bonusMinimo)
                .Where(h => string.IsNullOrWhiteSpace(nombre) || h.Nombre.Contains(nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.Id)
                .ToList();
        }
    }

    public Habilidad Obtener(int id)
    {
        lock (store.Sync)
        {
            return store.Habilidades.FirstOrDefault(h => h.Id == id)
                   ?? throw new NotFoundException($"No existe una habilidad con id {id}.");
        }
    }

    public Habilidad Crear(HabilidadRequest r)
    {
        var (nombre, tipo) = Validar(r);
        lock (store.Sync)
        {
            ValidarNombreUnico(nombre, null);
            var h = new Habilidad(store.NuevoIdHabilidad(), nombre, tipo, r.Descripcion?.Trim() ?? "", r.Bonus);
            store.Habilidades.Add(h);
            return h;
        }
    }

    public Habilidad Actualizar(int id, HabilidadRequest r)
    {
        var (nombre, tipo) = Validar(r);
        lock (store.Sync)
        {
            var idx = store.Habilidades.FindIndex(h => h.Id == id);
            if (idx < 0) throw new NotFoundException($"No existe una habilidad con id {id}.");
            ValidarNombreUnico(nombre, id);

            // Si pasa a ser de tipo Fuerza, quienes ya la tienen deben ser sensibles a la Fuerza.
            if (tipo == TipoHabilidad.Fuerza)
            {
                var invalidos = PersonajesAsignados(id).Where(p => !p.FuerzaSensitivo).Select(p => p.Nombre).ToList();
                if (invalidos.Count > 0)
                    throw new ReglaNegocioException(
                        $"No se puede cambiar a tipo Fuerza: la tienen personajes no sensibles a la Fuerza ({string.Join(", ", invalidos)}).");
            }

            var actualizada = store.Habilidades[idx] with
            {
                Nombre = nombre, Tipo = tipo, Descripcion = r.Descripcion?.Trim() ?? "", Bonus = r.Bonus
            };
            store.Habilidades[idx] = actualizada;
            return actualizada;
        }
    }

    public void Eliminar(int id)
    {
        lock (store.Sync)
        {
            var h = store.Habilidades.FirstOrDefault(x => x.Id == id)
                    ?? throw new NotFoundException($"No existe una habilidad con id {id}.");

            var usan = PersonajesAsignados(id).Select(p => p.Nombre).ToList();
            if (usan.Count > 0)
                throw new ReglaNegocioException(
                    $"No se puede eliminar '{h.Nombre}': la usan {string.Join(", ", usan)}. Quítela primero de esos personajes.");

            store.Habilidades.Remove(h);
        }
    }

    // ---------------- Relación Personaje ↔ Habilidad (N:N) ----------------

    public List<HabilidadAsignada> ListarPorPersonaje(int personajeId)
    {
        lock (store.Sync)
        {
            ObtenerPersonaje(personajeId);
            return ObtenerAsignadas(store, personajeId);
        }
    }

    public HabilidadAsignada Asignar(int personajeId, AsignarHabilidadRequest r)
    {
        if (r is null) throw new ReglaNegocioException("El cuerpo de la solicitud es obligatorio.");
        Validacion.Rango(r.Nivel, NivelMinimo, NivelMaximo, "nivel");

        lock (store.Sync)
        {
            var p = ObtenerPersonaje(personajeId);
            var h = store.Habilidades.FirstOrDefault(x => x.Id == r.HabilidadId)
                    ?? throw new ReglaNegocioException($"La habilidad {r.HabilidadId} no existe.");

            if (store.PersonajeHabilidades.Any(a => a.PersonajeId == personajeId && a.HabilidadId == h.Id))
                throw new ReglaNegocioException(
                    $"{p.Nombre} ya tiene la habilidad '{h.Nombre}'. Use PUT para cambiar su nivel.");

            if (h.Tipo == TipoHabilidad.Fuerza && !p.FuerzaSensitivo)
                throw new ReglaNegocioException(
                    $"{p.Nombre} no es sensible a la Fuerza y no puede aprender '{h.Nombre}' (tipo Fuerza).");

            store.PersonajeHabilidades.Add(
                new PersonajeHabilidad(store.NuevoIdAsignacion(), personajeId, h.Id, r.Nivel));
            return new HabilidadAsignada(h.Id, h.Nombre, h.Tipo, h.Descripcion, h.Bonus, r.Nivel);
        }
    }

    public HabilidadAsignada ActualizarNivel(int personajeId, int habilidadId, int nivel)
    {
        Validacion.Rango(nivel, NivelMinimo, NivelMaximo, "nivel");
        lock (store.Sync)
        {
            ObtenerPersonaje(personajeId);
            var idx = store.PersonajeHabilidades.FindIndex(a => a.PersonajeId == personajeId && a.HabilidadId == habilidadId);
            if (idx < 0)
                throw new NotFoundException($"El personaje {personajeId} no tiene la habilidad {habilidadId}.");

            store.PersonajeHabilidades[idx] = store.PersonajeHabilidades[idx] with { Nivel = nivel };
            var h = store.Habilidades.First(x => x.Id == habilidadId);
            return new HabilidadAsignada(h.Id, h.Nombre, h.Tipo, h.Descripcion, h.Bonus, nivel);
        }
    }

    public void Quitar(int personajeId, int habilidadId)
    {
        lock (store.Sync)
        {
            ObtenerPersonaje(personajeId);
            var quitadas = store.PersonajeHabilidades.RemoveAll(a => a.PersonajeId == personajeId && a.HabilidadId == habilidadId);
            if (quitadas == 0)
                throw new NotFoundException($"El personaje {personajeId} no tiene la habilidad {habilidadId}.");
        }
    }

    /// <summary>GET /habilidades/{id}/personajes</summary>
    public List<PersonajeConHabilidad> ListarPersonajes(int habilidadId)
    {
        lock (store.Sync)
        {
            if (!store.Habilidades.Any(h => h.Id == habilidadId))
                throw new NotFoundException($"No existe una habilidad con id {habilidadId}.");

            return store.PersonajeHabilidades
                .Where(a => a.HabilidadId == habilidadId)
                .Join(store.Personajes, a => a.PersonajeId, p => p.Id,
                    (a, p) => new PersonajeConHabilidad(p.Id, p.Nombre, p.Faccion, a.Nivel))
                .OrderByDescending(x => x.Nivel).ThenBy(x => x.Nombre)
                .ToList();
        }
    }

    // ---------------- Auxiliares ----------------

    private Personaje ObtenerPersonaje(int id) =>
        store.Personajes.FirstOrDefault(p => p.Id == id)
        ?? throw new NotFoundException($"No existe un personaje con id {id}.");

    private List<Personaje> PersonajesAsignados(int habilidadId) =>
        store.PersonajeHabilidades
            .Where(a => a.HabilidadId == habilidadId)
            .Join(store.Personajes, a => a.PersonajeId, p => p.Id, (a, p) => p)
            .ToList();

    private void ValidarNombreUnico(string nombre, int? idActual)
    {
        if (store.Habilidades.Any(h => h.Id != idActual && h.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
            throw new ReglaNegocioException($"Ya existe una habilidad llamada '{nombre}'.");
    }

    private static (string nombre, TipoHabilidad tipo) Validar(HabilidadRequest r)
    {
        if (r is null) throw new ReglaNegocioException("El cuerpo de la solicitud es obligatorio.");
        var nombre = Validacion.Requerido(r.Nombre, "nombre");
        if (r.Tipo is null)
            throw new ReglaNegocioException(
                $"El campo 'tipo' es obligatorio ({string.Join(", ", Enum.GetNames<TipoHabilidad>())}).");
        Validacion.Rango(r.Bonus, BonusMinimo, BonusMaximo, "bonus");
        return (nombre, r.Tipo.Value);
    }
}
