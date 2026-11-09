using System.Text.RegularExpressions;
using CatalogoGalactico.Models;

namespace CatalogoGalactico.Services;

/// <summary>Utilidades de validación y de conversión de la convención de fechas ABY/BBY.</summary>
public static class Validacion
{
    public static string Requerido(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ReglaNegocioException($"El campo '{campo}' es obligatorio.");
        return valor.Trim();
    }

    public static void Rango(int valor, int min, int max, string campo)
    {
        if (valor < min || valor > max)
            throw new ReglaNegocioException($"El campo '{campo}' debe estar entre {min} y {max}.");
    }

    public static T? ParseEnumOpcional<T>(string? valor, string campo) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (Enum.TryParse<T>(valor.Trim(), ignoreCase: true, out var r) && Enum.IsDefined(r)) return r;
        throw new ReglaNegocioException(
            $"Valor inválido para '{campo}': '{valor}'. Valores permitidos: {string.Join(", ", Enum.GetNames<T>())}.");
    }
}

/// <summary>
/// Convención temporal: 10 BBY = -10, 5 BBY = -5, Batalla de Yavin = 0, 3 ABY = 3, 10 ABY = 10.
/// </summary>
public static class FechaGalactica
{
    private static readonly Regex Patron =
        new(@"^\s*(\d{1,6})\s*(BBY|ABY)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int Parse(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new ReglaNegocioException("La fecha es obligatoria (ej.: '10 BBY', '3 ABY' o 'Batalla de Yavin').");

        var t = texto.Trim();
        if (t.Equals("Batalla de Yavin", StringComparison.OrdinalIgnoreCase)) return 0;

        var m = Patron.Match(t);
        if (m.Success)
        {
            var n = int.Parse(m.Groups[1].Value);
            return m.Groups[2].Value.Equals("BBY", StringComparison.OrdinalIgnoreCase) ? -n : n;
        }

        if (int.TryParse(t, out var anio) && Math.Abs(anio) <= 999_999) return anio;

        throw new ReglaNegocioException(
            $"Fecha inválida: '{texto}'. Use '10 BBY', '3 ABY', 'Batalla de Yavin' o un entero (-10, 0, 3).");
    }

    public static string Format(int anio) =>
        anio < 0 ? $"{-anio} BBY" : anio == 0 ? "Batalla de Yavin" : $"{anio} ABY";
}
