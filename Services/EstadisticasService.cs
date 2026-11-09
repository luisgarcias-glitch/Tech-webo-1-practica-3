using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class EstadisticasService(DataStore store)
{
    /// <summary>GET /personajes/ranking?por=poder|peligrosidad&amp;orden=desc|asc</summary>
    public List<RankingItem> Ranking(string? por, string? orden)
    {
        var criterio = string.IsNullOrWhiteSpace(por) ? "poder" : por.Trim().ToLowerInvariant();
        var direccion = string.IsNullOrWhiteSpace(orden) ? "desc" : orden.Trim().ToLowerInvariant();

        if (criterio is not ("poder" or "peligrosidad"))
            throw new ReglaNegocioException($"Criterio 'por' inválido: '{por}'. Valores permitidos: poder, peligrosidad.");
        if (direccion is not ("asc" or "desc"))
            throw new ReglaNegocioException($"Valor 'orden' inválido: '{orden}'. Valores permitidos: asc, desc.");

        lock (store.Sync)
        {
            var filas = store.Cartas
                .Join(store.Personajes, c => c.PersonajeId, p => p.Id, (c, p) => (P: p, C: c))
                .ToList();

            Func<(Personaje P, CardPersonaje C), int> clave =
                criterio == "poder" ? x => x.C.Poder : x => x.C.NivelPeligrosidad;

            var ordenadas = direccion == "desc"
                ? filas.OrderByDescending(clave).ThenBy(x => x.P.Nombre)
                : filas.OrderBy(clave).ThenBy(x => x.P.Nombre);

            return ordenadas
                .Select((x, i) => new RankingItem(i + 1, x.P.Id, x.P.Nombre, x.P.Faccion, x.C.Poder, x.C.NivelPeligrosidad))
                .ToList();
        }
    }

    /// <summary>GET /eventos/{id}/mvp: participante con mayor poder de carta.</summary>
    public MvpResponse Mvp(int eventoId)
    {
        lock (store.Sync)
        {
            var evento = store.Eventos.FirstOrDefault(e => e.Id == eventoId)
                         ?? throw new NotFoundException($"No existe un evento con id {eventoId}.");

            if (evento.Participantes.Count == 0)
                throw new ReglaNegocioException($"El evento '{evento.Nombre}' no tiene participantes.");

            var conCarta = new List<(Personaje P, CardPersonaje C)>();
            var sinCarta = new List<string>();
            foreach (var pid in evento.Participantes)
            {
                var p = store.Personajes.First(x => x.Id == pid);
                var c = store.Cartas.FirstOrDefault(x => x.PersonajeId == pid);
                if (c is null) sinCarta.Add(p.Nombre); else conCarta.Add((p, c));
            }

            if (conCarta.Count == 0)
                throw new ReglaNegocioException("Ningún participante del evento tiene carta; no se puede calcular el MVP.");

            var mvp = conCarta
                .OrderByDescending(x => x.C.Poder)
                .ThenByDescending(x => x.C.NivelPeligrosidad)
                .ThenBy(x => x.P.Id)
                .First();

            return new MvpResponse(evento.Id, evento.Nombre, mvp.P, mvp.C, sinCarta,
                "Mayor poder de carta entre los participantes; desempate por nivel de peligrosidad y luego por id.");
        }
    }
}
