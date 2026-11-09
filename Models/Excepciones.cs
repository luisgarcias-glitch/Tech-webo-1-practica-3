namespace CatalogoGalactico.Models;

/// <summary>El recurso solicitado no existe (HTTP 404).</summary>
public class NotFoundException(string mensaje) : Exception(mensaje);

/// <summary>Datos inválidos o regla de negocio incumplida (HTTP 400).</summary>
public class ReglaNegocioException(string mensaje) : Exception(mensaje);
