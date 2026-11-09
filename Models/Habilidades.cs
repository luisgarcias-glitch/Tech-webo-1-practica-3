using System.Text.Json.Serialization;

namespace CatalogoGalactico.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoHabilidad { Fuerza, Combate, Tecnica, Liderazgo, Pilotaje }

/// <summary>Catálogo independiente de habilidades.</summary>
public record Habilidad(int Id, string Nombre, TipoHabilidad Tipo, string Descripcion, int Bonus);

/// <summary>Tabla intermedia N:N entre Personajes y Habilidades; lleva el nivel de dominio (1-5).</summary>
public record PersonajeHabilidad(int Id, int PersonajeId, int HabilidadId, int Nivel);

// ---------- Solicitudes ----------
public record HabilidadRequest(string Nombre, TipoHabilidad? Tipo, string? Descripcion, int Bonus);
public record AsignarHabilidadRequest(int HabilidadId, int Nivel);
public record ActualizarNivelRequest(int Nivel);

// ---------- Respuestas ----------
/// <summary>Habilidad vista desde un personaje (catálogo + nivel).</summary>
public record HabilidadAsignada(int HabilidadId, string Nombre, TipoHabilidad Tipo, string Descripcion, int Bonus, int Nivel);

/// <summary>Personaje visto desde una habilidad.</summary>
public record PersonajeConHabilidad(int PersonajeId, string Nombre, Faccion Faccion, int Nivel);
