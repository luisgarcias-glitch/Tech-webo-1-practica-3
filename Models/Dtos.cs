namespace CatalogoGalactico.Models;

// ---------- Solicitudes ----------
public record PersonajeRequest(
    string Nombre,
    string Especie,
    Faccion? Faccion,
    string Afiliacion,
    EstadoPersonaje? Estado,
    bool FuerzaSensitivo,
    string? Image);

public record CardRequest(
    int PersonajeId,
    int Poder,
    string HabilidadEspecial,
    string Arma,
    int NivelPeligrosidad,
    string? ImagenUrl);

/// <param name="Fecha">"10 BBY", "3 ABY", "Batalla de Yavin" o un entero (-10, 0, 3).</param>
/// <param name="Participantes">IDs de personajes que participan.</param>
/// <param name="PersonajesMuertos">IDs de participantes que mueren en este evento.</param>
public record EventoRequest(
    string Nombre,
    string Fecha,
    string Ubicacion,
    string? Descripcion,
    List<int>? Participantes,
    List<int>? PersonajesMuertos);

// ---------- Respuestas ----------
public record EventoResponse(
    int Id,
    string Nombre,
    string Fecha,
    int AnioGuardado,
    string Ubicacion,
    string Descripcion,
    IReadOnlyList<int> Participantes,
    IReadOnlyList<int> PersonajesMuertos,
    string? Resultado,
    string? Ganador);

public record ParticipanteSimulado(int PersonajeId, string Nombre, int CardId, int Poder);

public record BandoSimulado(
    Faccion Bando,
    int Combatientes,
    int FuerzaBase,
    double FactorAleatorio,
    double FuerzaFinal,
    List<ParticipanteSimulado> Participantes);

public record SimulacionResponse(
    int EventoId,
    string NombreEvento,
    BandoSimulado Rebelde,
    BandoSimulado Imperio,
    List<string> NoCombatientes,
    string Ganador,
    string Resultado,
    string Criterio,
    int? Semilla);

public record RankingItem(
    int Posicion,
    int PersonajeId,
    string Nombre,
    Faccion Faccion,
    int Poder,
    int NivelPeligrosidad);

public record MvpResponse(
    int EventoId,
    string NombreEvento,
    Personaje Personaje,
    CardPersonaje Carta,
    List<string> ParticipantesSinCarta,
    string Criterio);

/// <summary>Ficha de combate: datos del personaje combinados con los de su carta (dos fuentes de datos).</summary>
public record PersonajeConCardResponse(
    int Id,
    string Nombre,
    string Especie,
    Faccion Faccion,
    string Afiliacion,
    EstadoPersonaje Estado,
    bool FuerzaSensitivo,
    string? Image,
    int Poder,
    string HabilidadEspecial,
    string Arma,
    int NivelPeligrosidad,
    CardPersonaje Card,
    List<HabilidadAsignada> Habilidades);
