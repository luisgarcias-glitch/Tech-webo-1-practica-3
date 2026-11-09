using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class EventoService(DataStore store, PersonajeService personajes)
{
    public static EventoResponse ToResponse(Evento e) => new(
        e.Id, e.Nombre, FechaGalactica.Format(e.Fecha), e.Fecha, e.Ubicacion, e.Descripcion,
        e.Participantes, e.PersonajesMuertos, e.Resultado, e.Ganador);

    public List<EventoResponse> Listar(string? nombre, string? ubicacion, int? personajeId, string? desde, string? hasta)
    {
        int? anioDesde = string.IsNullOrWhiteSpace(desde) ? null : FechaGalactica.Parse(desde);
        int? anioHasta = string.IsNullOrWhiteSpace(hasta) ? null : FechaGalactica.Parse(hasta);
        if (anioDesde > anioHasta)
            throw new ReglaNegocioException("El filtro 'desde' no puede ser posterior a 'hasta'.");

        lock (store.Sync)
        {
            if (personajeId is not null && !store.Personajes.Any(p => p.Id == personajeId))
                throw new NotFoundException($"No existe un personaje con id {personajeId}.");

            return store.Eventos
                .Where(e => personajeId is null || e.Participantes.Contains(personajeId.Value))
                .Where(e => anioDesde is null || e.Fecha >= anioDesde)
                .Where(e => anioHasta is null || e.Fecha <= anioHasta)
                .Where(e => string.IsNullOrWhiteSpace(nombre) || e.Nombre.Contains(nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(e => string.IsNullOrWhiteSpace(ubicacion) || e.Ubicacion.Contains(ubicacion.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Fecha).ThenBy(e => e.Id)
                .Select(ToResponse)
                .ToList();
        }
    }

    public EventoResponse Obtener(int id)
    {
        lock (store.Sync) return ToResponse(ObtenerEntidad(id));
    }

    public Evento ObtenerEntidad(int id)
    {
        lock (store.Sync)
        {
            return store.Eventos.FirstOrDefault(e => e.Id == id)
                   ?? throw new NotFoundException($"No existe un evento con id {id}.");
        }
    }

    /// <summary>GET /personajes/{id}/eventos: eventos en los que aparece el personaje, en orden cronológico.</summary>
    public List<EventoResponse> ListarPorPersonaje(int personajeId)
    {
        lock (store.Sync)
        {
            if (!store.Personajes.Any(p => p.Id == personajeId))
                throw new NotFoundException($"No existe un personaje con id {personajeId}.");

            return store.Eventos
                .Where(e => e.Participantes.Contains(personajeId))
                .OrderBy(e => e.Fecha).ThenBy(e => e.Id)
                .Select(ToResponse)
                .ToList();
        }
    }

    public EventoResponse Crear(EventoRequest r)
    {
        lock (store.Sync)
        {
            var candidato = Construir(-1, r);
            ValidarConsistenciaTemporal(store.Eventos.Append(candidato));

            var nuevo = candidato with { Id = store.NuevoIdEvento() };
            store.Eventos.Add(nuevo);
            SincronizarEstados(nuevo.PersonajesMuertos, Array.Empty<int>());
            return ToResponse(nuevo);
        }
    }

    public EventoResponse Actualizar(int id, EventoRequest r)
    {
        lock (store.Sync)
        {
            var idx = store.Eventos.FindIndex(e => e.Id == id);
            if (idx < 0) throw new NotFoundException($"No existe un evento con id {id}.");
            var previo = store.Eventos[idx];

            var candidato = Construir(id, r) with { Resultado = previo.Resultado, Ganador = previo.Ganador };

            // Si cambian los participantes, el resultado simulado anterior deja de ser válido.
            if (!previo.Participantes.OrderBy(x => x).SequenceEqual(candidato.Participantes.OrderBy(x => x)))
                candidato = candidato with { Resultado = null, Ganador = null };

            ValidarConsistenciaTemporal(store.Eventos.Where(e => e.Id != id).Append(candidato));

            store.Eventos[idx] = candidato;
            SincronizarEstados(
                candidato.PersonajesMuertos.Except(previo.PersonajesMuertos),
                previo.PersonajesMuertos.Except(candidato.PersonajesMuertos));
            return ToResponse(candidato);
        }
    }

    // ---------------- Reglas de negocio ----------------

    private Evento Construir(int id, EventoRequest r)
    {
        if (r is null) throw new ReglaNegocioException("El cuerpo de la solicitud es obligatorio.");
        var nombre = Validacion.Requerido(r.Nombre, "nombre");
        var ubicacion = Validacion.Requerido(r.Ubicacion, "ubicacion");
        var anio = FechaGalactica.Parse(r.Fecha);

        var participantes = r.Participantes ?? new List<int>();
        var muertos = r.PersonajesMuertos ?? new List<int>();

        if (participantes.Count != participantes.Distinct().Count())
            throw new ReglaNegocioException("La lista de participantes contiene IDs repetidos.");
        if (muertos.Count != muertos.Distinct().Count())
            throw new ReglaNegocioException("La lista de personajesMuertos contiene IDs repetidos.");

        foreach (var pid in participantes)
            if (!store.Personajes.Any(p => p.Id == pid))
                throw new ReglaNegocioException($"El participante con id {pid} no existe.");

        foreach (var pid in muertos)
            if (!participantes.Contains(pid))
                throw new ReglaNegocioException(
                    $"El personaje {NombreDe(pid)} figura como muerto pero no es participante del evento.");

        return new Evento(id, nombre, anio, ubicacion, r.Descripcion?.Trim() ?? "",
            participantes.ToList(), muertos.ToList(), null, null);
    }

    /// <summary>
    /// Valida sobre la lista completa de eventos que:
    /// 1) un personaje muere una sola vez, y
    /// 2) ningún personaje participa en un evento posterior a su muerte consignada.
    /// Un evento del mismo año que la muerte no se considera posterior.
    /// </summary>
    private void ValidarConsistenciaTemporal(IEnumerable<Evento> todos)
    {
        var lista = todos.ToList();
        var muertes = new Dictionary<int, Evento>();

        foreach (var ev in lista)
            foreach (var pid in ev.PersonajesMuertos)
            {
                if (muertes.TryGetValue(pid, out var previa))
                    throw new ReglaNegocioException(
                        $"{NombreDe(pid)} ya tiene su muerte consignada en '{previa.Nombre}'; " +
                        $"no puede morir de nuevo en '{ev.Nombre}'.");
                muertes[pid] = ev;
            }

        foreach (var ev in lista)
            foreach (var pid in ev.Participantes)
                if (muertes.TryGetValue(pid, out var muerte) && !ReferenceEquals(ev, muerte) && ev.Fecha > muerte.Fecha)
                    throw new ReglaNegocioException(
                        $"{NombreDe(pid)} no puede participar en '{ev.Nombre}' ({FechaGalactica.Format(ev.Fecha)}): " +
                        $"murió en '{muerte.Nombre}' ({FechaGalactica.Format(muerte.Fecha)}).");
    }

    /// <summary>Mantiene coherentes las colecciones: muertes nuevas → Muerto; muertes retiradas → Vivo.</summary>
    private void SincronizarEstados(IEnumerable<int> nuevosMuertos, IEnumerable<int> retirados)
    {
        foreach (var pid in nuevosMuertos)
            personajes.CambiarEstado(pid, EstadoPersonaje.Muerto);

        foreach (var pid in retirados)
            if (!store.Eventos.Any(e => e.PersonajesMuertos.Contains(pid)))
                personajes.CambiarEstado(pid, EstadoPersonaje.Vivo);
    }

    private string NombreDe(int personajeId) =>
        store.Personajes.FirstOrDefault(p => p.Id == personajeId)?.Nombre ?? $"Personaje {personajeId}";
}
