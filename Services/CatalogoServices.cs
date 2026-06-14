using EscuelaApp.Models;

namespace EscuelaApp.Services;

// ══════════════════════════════════════════════════════════════════════════════
//  ALUMNO SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class AlumnoService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Alumno>> ObtenerTodosAsync(string? busqueda = null)
    {
        var opciones = new QueryOptions { OrdenarPor = "ApellidoPaterno" };

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            opciones.Filtros.Add(new QueryFilter {
                Campo = "LOWER(Nombre || ' ' || ApellidoPaterno || ' ' || ApellidoMaterno || ' ' || Matricula)",
                Operador = "LIKE",
                Valor = busqueda.ToLower()
            });
        }

        var filas = await _q.EjecutarAsync("Alumnos", opciones);
        return filas.Select(MapearAlumno).ToList();
    }

    public async Task<Alumno?> ObtenerPorIdAsync(int id)
    {
        var opciones = new QueryOptions {
            Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
        };
        var filas = await _q.EjecutarAsync("Alumnos", opciones);
        return filas.Count > 0 ? MapearAlumno(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Alumno alumno)
    {
        var valores = new Dictionary<string, object?> {
            ["Matricula"]         = alumno.Matricula,
            ["Nombre"]            = alumno.Nombre,
            ["ApellidoPaterno"]   = alumno.ApellidoPaterno,
            ["ApellidoMaterno"]   = alumno.ApellidoMaterno,
            ["FechaNacimiento"]   = alumno.FechaNacimiento.ToString("yyyy-MM-dd"),
            ["Genero"]            = alumno.Genero,
            ["Email"]             = alumno.Email,
            ["Telefono"]          = alumno.Telefono,
            ["Direccion"]         = alumno.Direccion,
            ["Activo"]            = alumno.Activo ? 1 : 0,
            ["FechaRegistro"]     = alumno.FechaRegistro.ToString("o")
        };

        if (alumno.Id == 0)
            return await _q.InsertarAsync("Alumnos", valores);

        await _q.ActualizarAsync("Alumnos", valores, new() { ["Id"] = alumno.Id });
        return alumno.Id;
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Alumnos", new() { ["Id"] = id });

    public async Task<int> ContarAsync()
    {
        var r = await _q.EscalarAsync("SELECT COUNT(*) FROM Alumnos WHERE Activo = 1");
        return Convert.ToInt32(r);
    }

    public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
    {
        var lista = await ObtenerTodosAsync(busqueda);
        return excel.ExportarAlumnos(lista);
    }

    private static Alumno MapearAlumno(Dictionary<string, object?> row) => new()
    {
        Id              = Convert.ToInt32(row["Id"]),
        Matricula       = row["Matricula"]?.ToString() ?? "",
        Nombre          = row["Nombre"]?.ToString() ?? "",
        ApellidoPaterno = row["ApellidoPaterno"]?.ToString() ?? "",
        ApellidoMaterno = row["ApellidoMaterno"]?.ToString() ?? "",
        FechaNacimiento = DateTime.TryParse(row["FechaNacimiento"]?.ToString(), out var fn) ? fn : DateTime.MinValue,
        Genero          = row["Genero"]?.ToString() ?? "",
        Email           = row["Email"]?.ToString() ?? "",
        Telefono        = row["Telefono"]?.ToString() ?? "",
        Direccion       = row["Direccion"]?.ToString() ?? "",
        Activo          = Convert.ToInt32(row["Activo"] ?? 0) == 1,
        FechaRegistro   = DateTime.TryParse(row["FechaRegistro"]?.ToString(), out var fr) ? fr : DateTime.Now
    };
}

// ══════════════════════════════════════════════════════════════════════════════
//  MAESTRO SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class MaestroService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Maestro>> ObtenerTodosAsync(string? busqueda = null)
    {
        var opciones = new QueryOptions { OrdenarPor = "ApellidoPaterno" };

        if (!string.IsNullOrWhiteSpace(busqueda))
            opciones.Filtros.Add(new QueryFilter {
                Campo = "LOWER(Nombre || ' ' || ApellidoPaterno || ' ' || NumeroEmpleado)",
                Operador = "LIKE",
                Valor = busqueda.ToLower()
            });

        var filas = await _q.EjecutarAsync("Maestros", opciones);
        return filas.Select(MapearMaestro).ToList();
    }

    public async Task<Maestro?> ObtenerPorIdAsync(int id)
    {
        var filas = await _q.EjecutarAsync("Maestros", new QueryOptions {
            Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
        });
        return filas.Count > 0 ? MapearMaestro(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Maestro maestro)
    {
        var valores = new Dictionary<string, object?> {
            ["NumeroEmpleado"]  = maestro.NumeroEmpleado,
            ["Nombre"]          = maestro.Nombre,
            ["ApellidoPaterno"] = maestro.ApellidoPaterno,
            ["ApellidoMaterno"] = maestro.ApellidoMaterno,
            ["Especialidad"]    = maestro.Especialidad,
            ["Email"]           = maestro.Email,
            ["Telefono"]        = maestro.Telefono,
            ["Activo"]          = maestro.Activo ? 1 : 0,
            ["FechaRegistro"]   = maestro.FechaRegistro.ToString("o")
        };

        if (maestro.Id == 0) return await _q.InsertarAsync("Maestros", valores);
        await _q.ActualizarAsync("Maestros", valores, new() { ["Id"] = maestro.Id });
        return maestro.Id;
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Maestros", new() { ["Id"] = id });

    public async Task<int> ContarAsync()
    {
        var r = await _q.EscalarAsync("SELECT COUNT(*) FROM Maestros WHERE Activo = 1");
        return Convert.ToInt32(r);
    }

    public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
    {
        var lista = await ObtenerTodosAsync(busqueda);
        return excel.ExportarMaestros(lista);
    }

    private static Maestro MapearMaestro(Dictionary<string, object?> row) => new()
    {
        Id              = Convert.ToInt32(row["Id"]),
        NumeroEmpleado  = row["NumeroEmpleado"]?.ToString() ?? "",
        Nombre          = row["Nombre"]?.ToString() ?? "",
        ApellidoPaterno = row["ApellidoPaterno"]?.ToString() ?? "",
        ApellidoMaterno = row["ApellidoMaterno"]?.ToString() ?? "",
        Especialidad    = row["Especialidad"]?.ToString() ?? "",
        Email           = row["Email"]?.ToString() ?? "",
        Telefono        = row["Telefono"]?.ToString() ?? "",
        Activo          = Convert.ToInt32(row["Activo"] ?? 0) == 1,
        FechaRegistro   = DateTime.TryParse(row["FechaRegistro"]?.ToString(), out var fr) ? fr : DateTime.Now
    };
}

// ══════════════════════════════════════════════════════════════════════════════
//  MATERIA SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class MateriaService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Materia>> ObtenerTodosAsync(string? busqueda = null)
    {
        var opciones = new QueryOptions { OrdenarPor = "Nombre" };

        if (!string.IsNullOrWhiteSpace(busqueda))
            opciones.Filtros.Add(new QueryFilter {
                Campo = "LOWER(Nombre || ' ' || Clave)",
                Operador = "LIKE",
                Valor = busqueda.ToLower()
            });

        var filas = await _q.EjecutarAsync("Materias", opciones);
        return filas.Select(MapearMateria).ToList();
    }

    public async Task<Materia?> ObtenerPorIdAsync(int id)
    {
        var filas = await _q.EjecutarAsync("Materias", new QueryOptions {
            Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = id }]
        });
        return filas.Count > 0 ? MapearMateria(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Materia materia)
    {
        var valores = new Dictionary<string, object?> {
            ["Clave"]          = materia.Clave,
            ["Nombre"]         = materia.Nombre,
            ["Descripcion"]    = materia.Descripcion,
            ["Creditos"]       = materia.Creditos,
            ["HorasSemanales"] = materia.HorasSemanales,
            ["Activo"]         = materia.Activo ? 1 : 0
        };

        if (materia.Id == 0) return await _q.InsertarAsync("Materias", valores);
        await _q.ActualizarAsync("Materias", valores, new() { ["Id"] = materia.Id });
        return materia.Id;
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Materias", new() { ["Id"] = id });

    public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
    {
        var lista = await ObtenerTodosAsync(busqueda);
        return excel.ExportarMaterias(lista);
    }

    private static Materia MapearMateria(Dictionary<string, object?> row) => new()
    {
        Id             = Convert.ToInt32(row["Id"]),
        Clave          = row["Clave"]?.ToString() ?? "",
        Nombre         = row["Nombre"]?.ToString() ?? "",
        Descripcion    = row["Descripcion"]?.ToString() ?? "",
        Creditos       = Convert.ToInt32(row["Creditos"] ?? 0),
        HorasSemanales = Convert.ToInt32(row["HorasSemanales"] ?? 0),
        Activo         = Convert.ToInt32(row["Activo"] ?? 0) == 1
    };
}

// ══════════════════════════════════════════════════════════════════════════════
//  GRUPO SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class GrupoService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Grupo>> ObtenerTodosAsync(string? busqueda = null)
    {
        var sql = @"
            SELECT g.*, (m.Nombre || ' ' || m.ApellidoPaterno) AS MaestroNombre
            FROM Grupos g
            LEFT JOIN Maestros m ON g.MaestroId = m.Id
            ORDER BY g.Nombre";

        var filas = await _q.QueryAsync(sql);

        if (!string.IsNullOrWhiteSpace(busqueda))
            filas = filas.Where(f =>
                (f["Nombre"]?.ToString() ?? "").Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                (f["Grado"]?.ToString() ?? "").Contains(busqueda, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        return filas.Select(MapearGrupo).ToList();
    }

    public async Task<Grupo?> ObtenerPorIdAsync(int id)
    {
        var sql = @"SELECT g.*, (m.Nombre || ' ' || m.ApellidoPaterno) AS MaestroNombre
                    FROM Grupos g
                    LEFT JOIN Maestros m ON g.MaestroId = m.Id
                    WHERE g.Id = @id";
        var filas = await _q.QueryAsync(sql, new() { ["@id"] = id });
        return filas.Count > 0 ? MapearGrupo(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Grupo grupo)
    {
        var valores = new Dictionary<string, object?> {
            ["Nombre"]          = grupo.Nombre,
            ["Grado"]           = grupo.Grado,
            ["Turno"]           = grupo.Turno,
            ["CicloEscolar"]    = grupo.CicloEscolar,
            ["MaestroId"]       = grupo.MaestroId,
            ["CapacidadMaxima"] = grupo.CapacidadMaxima,
            ["Activo"]          = grupo.Activo ? 1 : 0
        };

        if (grupo.Id == 0) return await _q.InsertarAsync("Grupos", valores);
        await _q.ActualizarAsync("Grupos", valores, new() { ["Id"] = grupo.Id });
        return grupo.Id;
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Grupos", new() { ["Id"] = id });

    public async Task<byte[]> ExportarExcelAsync(string? busqueda = null)
    {
        var lista = await ObtenerTodosAsync(busqueda);
        return excel.ExportarGrupos(lista);
    }

    private static Grupo MapearGrupo(Dictionary<string, object?> row) => new()
    {
        Id              = Convert.ToInt32(row["Id"]),
        Nombre          = row["Nombre"]?.ToString() ?? "",
        Grado           = row["Grado"]?.ToString() ?? "",
        Turno           = row["Turno"]?.ToString() ?? "",
        CicloEscolar    = Convert.ToInt32(row["CicloEscolar"] ?? 0),
        MaestroId       = Convert.ToInt32(row["MaestroId"] ?? 0),
        MaestroNombre   = row.ContainsKey("MaestroNombre") ? row["MaestroNombre"]?.ToString() ?? "" : "",
        CapacidadMaxima = Convert.ToInt32(row["CapacidadMaxima"] ?? 30),
        Activo          = Convert.ToInt32(row["Activo"] ?? 0) == 1
    };
}
