using System.Text.Json.Serialization;

namespace CatalogoGalactico.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Faccion { Rebelde, Imperio, Neutral }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EstadoPersonaje { Vivo, Muerto, Desconocido }

/// <summary>Información general y estado actual del personaje.</summary>
public record Personaje(
    int Id,
    string Nombre,
    string Especie,
    Faccion Faccion,
    string Afiliacion,
    EstadoPersonaje Estado,
    bool FuerzaSensitivo,
    string? Image);

/// <summary>Atributos de juego de un personaje (relación 1:1 mediante PersonajeId).</summary>
public record CardPersonaje(
    int Id,
    int PersonajeId,
    int Poder,
    string HabilidadEspecial,
    string Arma,
    int NivelPeligrosidad,
    string? ImagenUrl);

/// <summary>
/// Hecho narrativo. La fecha se guarda como año entero (BBY = negativo, ABY = positivo,
/// Batalla de Yavin = 0). Participantes es la parte N:N (lista de IDs de personajes).
/// </summary>
public record Evento(
    int Id,
    string Nombre,
    int Fecha,
    string Ubicacion,
    string Descripcion,
    IReadOnlyList<int> Participantes,
    IReadOnlyList<int> PersonajesMuertos,
    string? Resultado,
    string? Ganador);
