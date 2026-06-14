using Microsoft.Data.Sqlite;
using EscuelaApp.Models;
using System.Text;

namespace EscuelaApp.Services;

// ══════════════════════════════════════════════════════════════════════════════
//  OBJECT PATTERN QUERY — Motor genérico de consultas SQLite
// ══════════════════════════════════════════════════════════════════════════════

public class ObjectPatternQuery
{
    private readonly string _connectionString;

    public ObjectPatternQuery(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ── Construye SELECT dinámico a partir de QueryOptions ───────────────────
    public async Task<List<Dictionary<string, object?>>> EjecutarAsync(
        string tabla, QueryOptions opciones)
    {
        var sql = new StringBuilder();

        // SELECT
        var campos = opciones.Campos.Count > 0
            ? string.Join(", ", opciones.Campos)
            : "*";
        sql.Append($"SELECT {campos} FROM {tabla}");

        // WHERE
        var parametros = new Dictionary<string, object?>();
        if (opciones.Filtros.Count > 0)
        {
            var condiciones = new List<string>();
            int i = 0;
            foreach (var f in opciones.Filtros)
            {
                var paramName = $"@p{i}";
                var op = f.Operador?.ToUpper() ?? "=";

                if (op == "LIKE")
                {
                    condiciones.Add($"{f.Campo} LIKE {paramName}");
                    parametros[paramName] = $"%{f.Valor}%";
                }
                else if (op == "IN" && f.Valor is IEnumerable<object> lista)
                {
                    var inParams = lista.Select((_, idx) => $"@in{idx}").ToList();
                    condiciones.Add($"{f.Campo} IN ({string.Join(",", inParams)})");
                    var vals = lista.ToList();
                    for (int j = 0; j < vals.Count; j++)
                        parametros[$"@in{j}"] = vals[j];
                }
                else
                {
                    condiciones.Add($"{f.Campo} {op} {paramName}");
                    parametros[paramName] = f.Valor;
                }
                i++;
            }
            sql.Append(" WHERE " + string.Join(" AND ", condiciones));
        }

        // ORDER BY
        if (!string.IsNullOrEmpty(opciones.OrdenarPor))
        {
            sql.Append($" ORDER BY {opciones.OrdenarPor}");
            if (opciones.Descendente) sql.Append(" DESC");
        }

        // LIMIT / OFFSET
        if (opciones.Limite.HasValue)
        {
            sql.Append($" LIMIT {opciones.Limite}");
            if (opciones.Offset.HasValue)
                sql.Append($" OFFSET {opciones.Offset}");
        }

        return await QueryAsync(sql.ToString(), parametros);
    }

    // ── Ejecuta SQL raw con parámetros ──────────────────────────────────────
    public async Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql, Dictionary<string, object?>? parametros = null)
    {
        var resultados = new List<Dictionary<string, object?>>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AgregarParametros(cmd, parametros);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var fila = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            resultados.Add(fila);
        }
        return resultados;
    }

    // ── Ejecuta INSERT / UPDATE / DELETE ────────────────────────────────────
    public async Task<int> EjecutarComandoAsync(
        string sql, Dictionary<string, object?>? parametros = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AgregarParametros(cmd, parametros);
        return await cmd.ExecuteNonQueryAsync();
    }

    // ── Inserta un registro y retorna el ID generado ─────────────────────────
    public async Task<long> InsertarAsync(
        string tabla, Dictionary<string, object?> valores)
    {
        var campos = string.Join(", ", valores.Keys);
        var paramNames = string.Join(", ", valores.Keys.Select(k => $"@{k}"));
        var sql = $"INSERT INTO {tabla} ({campos}) VALUES ({paramNames}); SELECT last_insert_rowid();";

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var kv in valores)
            cmd.Parameters.AddWithValue($"@{kv.Key}", kv.Value ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return result is long id ? id : 0;
    }

    // ── Actualiza registros por condición ────────────────────────────────────
    public async Task<int> ActualizarAsync(
        string tabla,
        Dictionary<string, object?> valores,
        Dictionary<string, object?> condicion)
    {
        var sets = string.Join(", ", valores.Keys.Select(k => $"{k} = @set_{k}"));
        var where = string.Join(" AND ", condicion.Keys.Select(k => $"{k} = @wh_{k}"));
        var sql = $"UPDATE {tabla} SET {sets} WHERE {where}";

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var kv in valores)
            cmd.Parameters.AddWithValue($"@set_{kv.Key}", kv.Value ?? DBNull.Value);
        foreach (var kv in condicion)
            cmd.Parameters.AddWithValue($"@wh_{kv.Key}", kv.Value ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync();
    }

    // ── Elimina registros por condición ─────────────────────────────────────
    public async Task<int> EliminarAsync(
        string tabla, Dictionary<string, object?> condicion)
    {
        var where = string.Join(" AND ", condicion.Keys.Select(k => $"{k} = @{k}"));
        var sql = $"DELETE FROM {tabla} WHERE {where}";
        return await EjecutarComandoAsync(sql, condicion.ToDictionary(
            kv => $"@{kv.Key}", kv => kv.Value));
    }

    // ── Escalar (COUNT, SUM, etc.) ───────────────────────────────────────────
    public async Task<object?> EscalarAsync(
        string sql, Dictionary<string, object?>? parametros = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AgregarParametros(cmd, parametros);
        return await cmd.ExecuteScalarAsync();
    }

    private static void AgregarParametros(
        SqliteCommand cmd, Dictionary<string, object?>? parametros)
    {
        if (parametros == null) return;
        foreach (var kv in parametros)
            cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  DATABASE SERVICE — Inicialización y seed
// ══════════════════════════════════════════════════════════════════════════════

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

        // Maestros seed
        var maestros = new[] {
            ("EMP001","Ana","García","López","Matemáticas","ana.garcia@escuela.edu","555-0101"),
            ("EMP002","Carlos","Martínez","Ruiz","Ciencias","carlos.martinez@escuela.edu","555-0102"),
            ("EMP003","María","López","Hernández","Historia","maria.lopez@escuela.edu","555-0103"),
        };
        foreach (var (num, nom, ap, am, esp, email, tel) in maestros)
            await Query.InsertarAsync("Maestros", new Dictionary<string, object?> {
                ["NumeroEmpleado"] = num, ["Nombre"] = nom,
                ["ApellidoPaterno"] = ap, ["ApellidoMaterno"] = am,
                ["Especialidad"] = esp, ["Email"] = email,
                ["Telefono"] = tel, ["Activo"] = 1,
                ["FechaRegistro"] = DateTime.Now.ToString("o")
            });

        // Materias seed
        var materias = new[] {
            ("MAT01","Matemáticas I","Álgebra y geometría",8,4),
            ("ESP01","Español I","Gramática y redacción",8,4),
            ("HIST01","Historia","Historia universal",6,3),
            ("FISI01","Física I","Mecánica clásica",8,4),
            ("QUIM01","Química I","Química general",8,4),
        };
        foreach (var (clave, nom, desc, cred, hrs) in materias)
            await Query.InsertarAsync("Materias", new Dictionary<string, object?> {
                ["Clave"] = clave, ["Nombre"] = nom, ["Descripcion"] = desc,
                ["Creditos"] = cred, ["HorasSemanales"] = hrs, ["Activo"] = 1
            });

        // Grupos seed
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?> {
            ["Nombre"] = "1-A", ["Grado"] = "Primero", ["Turno"] = "Matutino",
            ["CicloEscolar"] = 2025, ["MaestroId"] = 1, ["CapacidadMaxima"] = 30, ["Activo"] = 1
        });
        await Query.InsertarAsync("Grupos", new Dictionary<string, object?> {
            ["Nombre"] = "2-B", ["Grado"] = "Segundo", ["Turno"] = "Vespertino",
            ["CicloEscolar"] = 2025, ["MaestroId"] = 2, ["CapacidadMaxima"] = 35, ["Activo"] = 1
        });

        // Alumnos seed
        var alumnos = new[] {
            ("2025001","Juan","Pérez","González","2008-03-15","M","juan.perez@email.com","555-1001"),
            ("2025002","Sofía","Ramírez","Torres","2008-07-22","F","sofia.ramirez@email.com","555-1002"),
            ("2025003","Diego","Hernández","Flores","2008-11-05","M","diego.hernandez@email.com","555-1003"),
            ("2025004","Valentina","Cruz","Morales","2009-01-30","F","valentina.cruz@email.com","555-1004"),
            ("2025005","Mateo","Jiménez","Vega","2008-09-18","M","mateo.jimenez@email.com","555-1005"),
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
