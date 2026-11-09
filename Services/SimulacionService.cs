using CatalogoGalactico.Data;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

public class SimulacionService(DataStore store)
{
    public const double FactorMinimo = 0.90;
    public const double FactorMaximo = 1.10;

    /// <summary>
    /// Suma el poder de las cartas por bando (Rebelde / Imperio), aplica a cada bando un factor
    /// aleatorio en [0.90, 1.10] y declara ganador al bando con mayor fuerza final.
    /// Con la misma semilla el resultado es idéntico. No guarda nada si hay algún error.
    /// </summary>
    public SimulacionResponse Simular(int eventoId, int? semilla)
    {
        lock (store.Sync)
        {
            var idx = store.Eventos.FindIndex(e => e.Id == eventoId);
            if (idx < 0) throw new NotFoundException($"No existe un evento con id {eventoId}.");
            var evento = store.Eventos[idx];

            if (evento.Participantes.Count < 2)
                throw new ReglaNegocioException(
                    $"El evento '{evento.Nombre}' necesita al menos 2 participantes para simular una batalla.");

            var combatientes = new List<(Personaje P, CardPersonaje C)>();
            var sinCarta = new List<string>();
            foreach (var pid in evento.Participantes)
            {
                var p = store.Personajes.First(x => x.Id == pid);
                var c = store.Cartas.FirstOrDefault(x => x.PersonajeId == pid);
                if (c is null) sinCarta.Add(p.Nombre); else combatientes.Add((p, c));
            }
            if (sinCarta.Count > 0)
                throw new ReglaNegocioException(
                    $"No se puede simular: faltan cartas para {string.Join(", ", sinCarta)}.");

            var rebeldes = combatientes.Where(x => x.P.Faccion == Faccion.Rebelde).ToList();
            var imperiales = combatientes.Where(x => x.P.Faccion == Faccion.Imperio).ToList();
            var neutrales = combatientes.Where(x => x.P.Faccion == Faccion.Neutral).Select(x => x.P.Nombre).ToList();

            if (rebeldes.Count == 0 || imperiales.Count == 0)
                throw new ReglaNegocioException(
                    "No se puede simular: se necesita al menos un participante Rebelde y uno del Imperio " +
                    $"(hay {rebeldes.Count} rebeldes y {imperiales.Count} imperiales; los Neutrales no combaten).");

            var rnd = semilla is null ? new Random() : new Random(semilla.Value);
            var bandoR = ArmarBando(Faccion.Rebelde, rebeldes, rnd);
            var bandoI = ArmarBando(Faccion.Imperio, imperiales, rnd);

            string ganador = bandoR.FuerzaFinal > bandoI.FuerzaFinal ? "Rebelde"
                           : bandoI.FuerzaFinal > bandoR.FuerzaFinal ? "Imperio"
                           : "Empate";

            var resultado = ganador == "Empate"
                ? $"Empate: ambos bandos alcanzan {bandoR.FuerzaFinal}."
                : $"Victoria {ganador}: Rebelde {bandoR.FuerzaFinal} vs Imperio {bandoI.FuerzaFinal}.";

            var criterio =
                $"Fuerza base = suma del poder de las cartas de cada bando. Se multiplica por un factor aleatorio " +
                $"entre {FactorMinimo:0.00} y {FactorMaximo:0.00} (uno por bando). Gana la mayor fuerza final; " +
                "si son iguales, hay empate. Los personajes Neutrales no combaten.";

            // Solo se guarda el resultado cuando toda la simulación fue exitosa.
            store.Eventos[idx] = evento with { Resultado = resultado, Ganador = ganador };

            return new SimulacionResponse(evento.Id, evento.Nombre, bandoR, bandoI, neutrales,
                ganador, resultado, criterio, semilla);
        }
    }

    private static BandoSimulado ArmarBando(Faccion faccion, List<(Personaje P, CardPersonaje C)> miembros, Random rnd)
    {
        var baseFuerza = miembros.Sum(m => m.C.Poder);
        var factor = Math.Round(FactorMinimo + rnd.NextDouble() * (FactorMaximo - FactorMinimo), 4);
        var final = Math.Round(baseFuerza * factor, 2);
        var detalle = miembros
            .Select(m => new ParticipanteSimulado(m.P.Id, m.P.Nombre, m.C.Id, m.C.Poder))
            .ToList();
        return new BandoSimulado(faccion, miembros.Count, baseFuerza, factor, final, detalle);
    }
}
