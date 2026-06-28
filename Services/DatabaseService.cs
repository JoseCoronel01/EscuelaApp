using Microsoft.Data.Sqlite;

namespace EscuelaApp.Services;

public class DatabaseService
{
    public readonly string ConnectionString;
    public readonly ObjectPatternQuery Query;

    public DatabaseService(IConfiguration config)
    {
        var dbPath = config["Database:Path"] ?? "escuela.db";
        ConnectionString = $"Data Source={dbPath}";
        Query = new ObjectPatternQuery(ConnectionString);
    }

    public async Task InicializarAsync()
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = GetEsquema();
        await cmd.ExecuteNonQueryAsync();
        await SeedAsync();
    }

    private static string GetEsquema() => @"
        PRAGMA journal_mode=WAL;

        CREATE TABLE IF NOT EXISTS Alumnos (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            Matricula         TEXT NOT NULL UNIQUE,
            Nombre            TEXT NOT NULL,
            ApellidoPaterno   TEXT NOT NULL,
            ApellidoMaterno   TEXT,
            FechaNacimiento   TEXT NOT NULL,
            Genero            TEXT NOT NULL,
            Email             TEXT,
            Telefono          TEXT,
            Direccion         TEXT,
            Activo            INTEGER NOT NULL DEFAULT 1,
            FechaRegistro     TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Maestros (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            NumeroEmpleado    TEXT NOT NULL UNIQUE,
            Nombre            TEXT NOT NULL,
            ApellidoPaterno   TEXT NOT NULL,
            ApellidoMaterno   TEXT,
            Especialidad      TEXT,
            Email             TEXT,
            Telefono          TEXT,
            Activo            INTEGER NOT NULL DEFAULT 1,
            FechaRegistro     TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Materias (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            Clave            TEXT NOT NULL UNIQUE,
            Nombre           TEXT NOT NULL,
            Descripcion      TEXT,
            Creditos         INTEGER NOT NULL DEFAULT 0,
            HorasSemanales   INTEGER NOT NULL DEFAULT 0,
            Activo           INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS Grupos (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre           TEXT NOT NULL,
            Grado            TEXT NOT NULL,
            Turno            TEXT NOT NULL,
            CicloEscolar     INTEGER NOT NULL,
            MaestroId        INTEGER NOT NULL,
            CapacidadMaxima  INTEGER NOT NULL DEFAULT 30,
            Activo           INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (MaestroId) REFERENCES Maestros(Id)
        );

        CREATE TABLE IF NOT EXISTS Inscripciones (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            AlumnoId         INTEGER NOT NULL,
            GrupoId          INTEGER NOT NULL,
            MateriaId        INTEGER NOT NULL,
            FechaInscripcion TEXT NOT NULL DEFAULT (datetime('now')),
            Estado           TEXT NOT NULL DEFAULT 'Activa',
            CicloEscolar     TEXT NOT NULL,
            FOREIGN KEY (AlumnoId)  REFERENCES Alumnos(Id),
            FOREIGN KEY (GrupoId)   REFERENCES Grupos(Id),
            FOREIGN KEY (MateriaId) REFERENCES Materias(Id)
        );

        CREATE TABLE IF NOT EXISTS Calificaciones (
            Id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            InscripcionId        INTEGER NOT NULL,
            AlumnoId             INTEGER NOT NULL,
            MateriaId            INTEGER NOT NULL,
            GrupoId              INTEGER NOT NULL,
            Parcial1             REAL NOT NULL DEFAULT 0,
            Parcial2             REAL NOT NULL DEFAULT 0,
            Parcial3             REAL NOT NULL DEFAULT 0,
            Promedio             REAL NOT NULL DEFAULT 0,
            Estado               TEXT NOT NULL DEFAULT 'En Curso',
            Periodo              TEXT NOT NULL,
            Observaciones        TEXT,
            FechaActualizacion   TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
            FOREIGN KEY (AlumnoId)      REFERENCES Alumnos(Id),
            FOREIGN KEY (MateriaId)     REFERENCES Materias(Id),
            FOREIGN KEY (GrupoId)       REFERENCES Grupos(Id)
        );
    ";

    private async Task SeedAsync()
    {
        var count = await Query.EscalarAsync("SELECT COUNT(*) FROM Alumnos");
        if (Convert.ToInt64(count) > 0) return;

        var maestros = new[] {
            ("EMP001","Ana","García","López","Matemáticas","ana.garcia@escuela.edu","555-0101")
        };
        foreach (var (num, nom, ap, am, esp, email, tel) in maestros)
            await Query.InsertarAsync("Maestros", new Dictionary<string, object?> {
                ["NumeroEmpleado"] = num, ["Nombre"] = nom,
                ["ApellidoPaterno"] = ap, ["ApellidoMaterno"] = am,
                ["Especialidad"] = esp, ["Email"] = email,
                ["Telefono"] = tel, ["Activo"] = 1,
                ["FechaRegistro"] = DateTime.Now.ToString("o")
            });

        var materias = new[] {
            ("MAT01","Matemáticas I","Álgebra y geometría",8,4),
            ("ESP01","Español I","Gramática y redacción",8,4),
            ("HIST01","Historia","Historia universal",6,3),
            ("FISI01","Física I","Mecánica clásica",8,4),
            ("QUIM01","Química I","Química general",8,4),
            ("COMP01","Computación I","Computación",8,4),
        };
        foreach (var (clave, nom, desc, cred, hrs) in materias)
            await Query.InsertarAsync("Materias", new Dictionary<string, object?> {
                ["Clave"] = clave, ["Nombre"] = nom, ["Descripcion"] = desc,
                ["Creditos"] = cred, ["HorasSemanales"] = hrs, ["Activo"] = 1
            });

        await Query.InsertarAsync("Grupos", new Dictionary<string, object?> {
            ["Nombre"] = "1-A", ["Grado"] = "Primero", ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026, ["MaestroId"] = 1, ["CapacidadMaxima"] = 30, ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?> {
            ["Nombre"] = "2-A", ["Grado"] = "Segundo", ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026, ["MaestroId"] = 1, ["CapacidadMaxima"] = 30, ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?>
        {
            ["Nombre"] = "3-A",
            ["Grado"] = "Tercero",
            ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026,
            ["MaestroId"] = 1,
            ["CapacidadMaxima"] = 30,
            ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?>
        {
            ["Nombre"] = "4-A",
            ["Grado"] = "Cuarto",
            ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026,
            ["MaestroId"] = 1,
            ["CapacidadMaxima"] = 30,
            ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?>
        {
            ["Nombre"] = "5-A",
            ["Grado"] = "Quinto",
            ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026,
            ["MaestroId"] = 1,
            ["CapacidadMaxima"] = 30,
            ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?>
        {
            ["Nombre"] = "6-A",
            ["Grado"] = "Sexto",
            ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2026,
            ["MaestroId"] = 1,
            ["CapacidadMaxima"] = 30,
            ["Activo"] = 1
        });

        var alumnos = new[] {
            ("202610001","Juan","Pérez","González","2020-08-01","M","juan.perez@email.com","555-1001")
        };
        foreach (var (mat, nom, ap, am, fn, gen, email, tel) in alumnos)
            await Query.InsertarAsync("Alumnos", new Dictionary<string, object?> {
                ["Matricula"] = mat, ["Nombre"] = nom,
                ["ApellidoPaterno"] = ap, ["ApellidoMaterno"] = am,
                ["FechaNacimiento"] = fn, ["Genero"] = gen,
                ["Email"] = email, ["Telefono"] = tel,
                ["Activo"] = 1, ["FechaRegistro"] = DateTime.Now.ToString("o")
            });
    }
}
