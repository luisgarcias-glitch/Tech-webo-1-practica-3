using CatalogoGalactico.Models;

namespace CatalogoGalactico.Data;

/// <summary>
/// Almacenamiento en memoria: tres listas fijas con datos iniciales.
/// Se registra como singleton; al reiniciar la app los datos vuelven al estado inicial.
/// </summary>
public class DataStore
{
    /// <summary>Candado compartido por los servicios para operaciones atómicas.</summary>
    public object Sync { get; } = new();

    public List<Personaje> Personajes { get; } = new();
    public List<CardPersonaje> Cartas { get; } = new();
    public List<Evento> Eventos { get; } = new();
    public List<Habilidad> Habilidades { get; } = new();
    public List<PersonajeHabilidad> PersonajeHabilidades { get; } = new();

    private int _idPersonaje, _idCarta, _idEvento, _idHabilidad, _idAsignacion;

    public int NuevoIdPersonaje() => ++_idPersonaje;
    public int NuevoIdCarta() => ++_idCarta;
    public int NuevoIdEvento() => ++_idEvento;
    public int NuevoIdHabilidad() => ++_idHabilidad;
    public int NuevoIdAsignacion() => ++_idAsignacion;

    public DataStore() => Sembrar();

    private void Sembrar()
    {
        // Los estados iniciales ya son coherentes con las muertes consignadas en los eventos.
        Personajes.AddRange(new[]
        {
            new Personaje(1, "Luke Skywalker", "Humano", Faccion.Rebelde, "Alianza Rebelde", EstadoPersonaje.Vivo, true, "https://example.com/personajes/luke.png"),
            new Personaje(2, "Leia Organa", "Humana", Faccion.Rebelde, "Alianza Rebelde", EstadoPersonaje.Vivo, true, "https://example.com/personajes/leia.png"),
            new Personaje(3, "Han Solo", "Humano", Faccion.Rebelde, "Alianza Rebelde", EstadoPersonaje.Vivo, false, "https://example.com/personajes/han.png"),
            new Personaje(4, "Chewbacca", "Wookiee", Faccion.Rebelde, "Alianza Rebelde", EstadoPersonaje.Vivo, false, "https://example.com/personajes/chewbacca.png"),
            new Personaje(5, "Obi-Wan Kenobi", "Humano", Faccion.Rebelde, "Orden Jedi", EstadoPersonaje.Muerto, true, "https://example.com/personajes/obiwan.png"),
            new Personaje(6, "Yoda", "Desconocida", Faccion.Neutral, "Orden Jedi", EstadoPersonaje.Muerto, true, "https://example.com/personajes/yoda.png"),
            new Personaje(7, "Darth Vader", "Humano", Faccion.Imperio, "Imperio Galáctico", EstadoPersonaje.Muerto, true, "https://example.com/personajes/vader.png"),
            new Personaje(8, "Emperador Palpatine", "Humano", Faccion.Imperio, "Imperio Galáctico", EstadoPersonaje.Muerto, true, "https://example.com/personajes/palpatine.png"),
            new Personaje(9, "Wilhuff Tarkin", "Humano", Faccion.Imperio, "Imperio Galáctico", EstadoPersonaje.Muerto, false, "https://example.com/personajes/tarkin.png"),
            new Personaje(10, "Boba Fett", "Humano", Faccion.Neutral, "Cazarrecompensas", EstadoPersonaje.Desconocido, false, "https://example.com/personajes/boba.png"),
            new Personaje(11, "Lando Calrissian", "Humano", Faccion.Rebelde, "Alianza Rebelde", EstadoPersonaje.Vivo, false, "https://example.com/personajes/lando.png"),
        });
        _idPersonaje = 11;

        Cartas.AddRange(new[]
        {
            new CardPersonaje(1, 1, 88, "Empuje de la Fuerza", "Sable de luz verde", 8, "https://example.com/cartas/luke.png"),
            new CardPersonaje(2, 2, 55, "Liderazgo rebelde", "Bláster", 5, "https://example.com/cartas/leia.png"),
            new CardPersonaje(3, 3, 60, "Puntería veloz", "Bláster DL-44", 6, "https://example.com/cartas/han.png"),
            new CardPersonaje(4, 4, 72, "Fuerza wookiee", "Ballesta bowcaster", 7, "https://example.com/cartas/chewbacca.png"),
            new CardPersonaje(5, 5, 85, "Mente Jedi", "Sable de luz azul", 8, "https://example.com/cartas/obiwan.png"),
            new CardPersonaje(6, 6, 97, "Maestría de la Fuerza", "Sable de luz verde", 9, "https://example.com/cartas/yoda.png"),
            new CardPersonaje(7, 7, 95, "Estrangulamiento de la Fuerza", "Sable de luz rojo", 10, "https://example.com/cartas/vader.png"),
            new CardPersonaje(8, 8, 98, "Rayos de la Fuerza", "Rayos Sith", 10, "https://example.com/cartas/palpatine.png"),
            new CardPersonaje(9, 9, 45, "Estrategia imperial", "Superláser de la Estrella de la Muerte", 6, "https://example.com/cartas/tarkin.png"),
            new CardPersonaje(10, 10, 70, "Jetpack", "Bláster EE-3", 7, "https://example.com/cartas/boba.png"),
            new CardPersonaje(11, 11, 50, "Astucia de contrabandista", "Bláster", 4, "https://example.com/cartas/lando.png"),
        });
        _idCarta = 11;

        Eventos.AddRange(new[]
        {
            new Evento(1, "Duelo en Mustafar", -19, "Mustafar", "Duelo entre maestro y aprendiz junto a ríos de lava.",
                new[] { 5, 7 }, Array.Empty<int>(), null, null),
            new Evento(2, "Duelo en la Estrella de la Muerte", 0, "Estrella de la Muerte", "Obi-Wan se sacrifica para permitir el escape.",
                new[] { 5, 7 }, new[] { 5 }, null, null),
            new Evento(3, "Batalla de Yavin", 0, "Yavin 4", "Ataque rebelde que destruye la primera Estrella de la Muerte.",
                new[] { 1, 2, 3, 4, 7, 9 }, new[] { 9 }, null, null),
            new Evento(4, "Batalla de Hoth", 3, "Hoth", "El Imperio asalta la base Echo.",
                new[] { 1, 2, 3, 4, 7 }, Array.Empty<int>(), null, null),
            new Evento(5, "Muerte de Yoda", 4, "Dagobah", "Yoda se despide de Luke.",
                new[] { 1, 6 }, new[] { 6 }, null, null),
            new Evento(6, "Batalla de Endor", 4, "Endor", "Batalla final contra el Emperador y la segunda Estrella de la Muerte.",
                new[] { 1, 2, 3, 4, 7, 8, 11 }, new[] { 7, 8 }, null, null),
        });
        _idEvento = 6;

        // Catálogo de habilidades (las de tipo Fuerza solo se asignan a personajes sensibles a la Fuerza)
        Habilidades.AddRange(new[]
        {
            new Habilidad(1, "Empuje de la Fuerza", TipoHabilidad.Fuerza, "Mueve objetos y enemigos con la mente.", 15),
            new Habilidad(2, "Estrangulamiento de la Fuerza", TipoHabilidad.Fuerza, "Asfixia a distancia.", 18),
            new Habilidad(3, "Puntería veloz", TipoHabilidad.Combate, "Dispara con precisión bajo presión.", 8),
            new Habilidad(4, "Pilotaje experto", TipoHabilidad.Pilotaje, "Maniobras avanzadas en combate espacial.", 10),
            new Habilidad(5, "Liderazgo rebelde", TipoHabilidad.Liderazgo, "Inspira y coordina a las tropas.", 7),
            new Habilidad(6, "Duelo de sables", TipoHabilidad.Combate, "Maestría en combate con sable de luz.", 12),
            new Habilidad(7, "Reparación de naves", TipoHabilidad.Tecnica, "Repara sistemas en pleno vuelo.", 6),
            new Habilidad(8, "Rayos de la Fuerza", TipoHabilidad.Fuerza, "Descargas de energía oscura.", 20),
        });
        _idHabilidad = 8;

        var asignaciones = new (int Personaje, int Habilidad, int Nivel)[]
        {
            (1, 1, 4), (1, 4, 3), (1, 6, 4),      // Luke
            (2, 5, 5), (2, 1, 2),                 // Leia
            (3, 3, 4), (3, 4, 5), (3, 7, 3),      // Han
            (4, 7, 5), (4, 4, 4),                 // Chewbacca
            (5, 6, 5), (5, 1, 5),                 // Obi-Wan
            (6, 1, 5),                            // Yoda
            (7, 2, 5), (7, 6, 5),                 // Vader
            (8, 8, 5),                            // Palpatine
            (9, 5, 4),                            // Tarkin
            (10, 3, 5),                           // Boba Fett
            (11, 4, 4), (11, 5, 3),               // Lando
        };
        foreach (var a in asignaciones)
            PersonajeHabilidades.Add(new PersonajeHabilidad(NuevoIdAsignacion(), a.Personaje, a.Habilidad, a.Nivel));
    }
}
