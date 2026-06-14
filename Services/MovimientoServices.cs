using EscuelaApp.Models;

namespace EscuelaApp.Services;

// ══════════════════════════════════════════════════════════════════════════════
//  INSCRIPCION SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class InscripcionService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Inscripcion>> ObtenerTodosAsync(
        int? alumnoId = null, int? grupoId = null, string? ciclo = null)
    {
        var sql = @"
            SELECT i.*,
                   a.Nombre || ' ' || a.ApellidoPaterno || ' ' || COALESCE(a.ApellidoMaterno,'') AS AlumnoNombre,
                   a.Matricula AS AlumnoMatricula,
                   g.Nombre    AS GrupoNombre,
                   m.Nombre    AS MateriaNombre
            FROM Inscripciones i
            JOIN Alumnos  a ON i.AlumnoId  = a.Id
            JOIN Grupos   g ON i.GrupoId   = g.Id
            JOIN Materias m ON i.MateriaId = m.Id
            WHERE 1=1";

        var parametros = new Dictionary<string, object?>();

        if (alumnoId.HasValue)
        { sql += " AND i.AlumnoId = @AlumnoId"; parametros["@AlumnoId"] = alumnoId; }
        if (grupoId.HasValue)
        { sql += " AND i.GrupoId = @GrupoId"; parametros["@GrupoId"] = grupoId; }
        if (!string.IsNullOrWhiteSpace(ciclo))
        { sql += " AND i.CicloEscolar = @Ciclo"; parametros["@Ciclo"] = ciclo; }

        sql += " ORDER BY a.ApellidoPaterno, m.Nombre";

        var filas = await _q.QueryAsync(sql, parametros);
        return filas.Select(MapearInscripcion).ToList();
    }

    public async Task<Inscripcion?> ObtenerPorIdAsync(int id)
    {
        var sql = @"
            SELECT i.*,
                   a.Nombre || ' ' || a.ApellidoPaterno AS AlumnoNombre,
                   a.Matricula AS AlumnoMatricula,
                   g.Nombre    AS GrupoNombre,
                   m.Nombre    AS MateriaNombre
            FROM Inscripciones i
            JOIN Alumnos  a ON i.AlumnoId  = a.Id
            JOIN Grupos   g ON i.GrupoId   = g.Id
            JOIN Materias m ON i.MateriaId = m.Id
            WHERE i.Id = @id";

        var filas = await _q.QueryAsync(sql, new() { ["@id"] = id });
        return filas.Count > 0 ? MapearInscripcion(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Inscripcion inscripcion)
    {
        var valores = new Dictionary<string, object?> {
            ["AlumnoId"]         = inscripcion.AlumnoId,
            ["GrupoId"]          = inscripcion.GrupoId,
            ["MateriaId"]        = inscripcion.MateriaId,
            ["FechaInscripcion"] = inscripcion.FechaInscripcion.ToString("o"),
            ["Estado"]           = inscripcion.Estado,
            ["CicloEscolar"]     = inscripcion.CicloEscolar
        };

        if (inscripcion.Id == 0) return await _q.InsertarAsync("Inscripciones", valores);
        await _q.ActualizarAsync("Inscripciones", valores, new() { ["Id"] = inscripcion.Id });
        return inscripcion.Id;
    }

    public async Task CambiarEstadoAsync(int id, string nuevoEstado)
    {
        await _q.ActualizarAsync("Inscripciones",
            new() { ["Estado"] = nuevoEstado },
            new() { ["Id"] = id });
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Inscripciones", new() { ["Id"] = id });

    public async Task<int> ContarAsync()
    {
        var r = await _q.EscalarAsync("SELECT COUNT(*) FROM Inscripciones WHERE Estado = 'Activa'");
        return Convert.ToInt32(r);
    }

    public async Task<byte[]> ExportarExcelAsync(
        int? alumnoId = null, int? grupoId = null, string? ciclo = null)
    {
        var lista = await ObtenerTodosAsync(alumnoId, grupoId, ciclo);
        return excel.ExportarInscripciones(lista);
    }

    private static Inscripcion MapearInscripcion(Dictionary<string, object?> row) => new()
    {
        Id               = Convert.ToInt32(row["Id"]),
        AlumnoId         = Convert.ToInt32(row["AlumnoId"]),
        GrupoId          = Convert.ToInt32(row["GrupoId"]),
        MateriaId        = Convert.ToInt32(row["MateriaId"]),
        AlumnoNombre     = row["AlumnoNombre"]?.ToString() ?? "",
        AlumnoMatricula  = row["AlumnoMatricula"]?.ToString() ?? "",
        GrupoNombre      = row["GrupoNombre"]?.ToString() ?? "",
        MateriaNombre    = row["MateriaNombre"]?.ToString() ?? "",
        FechaInscripcion = DateTime.TryParse(row["FechaInscripcion"]?.ToString(), out var fi) ? fi : DateTime.Now,
        Estado           = row["Estado"]?.ToString() ?? "Activa",
        CicloEscolar     = row["CicloEscolar"]?.ToString() ?? ""
    };
}

// ══════════════════════════════════════════════════════════════════════════════
//  CALIFICACION SERVICE
// ══════════════════════════════════════════════════════════════════════════════

public class CalificacionService(DatabaseService db, ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<List<Calificacion>> ObtenerTodosAsync(
        int? alumnoId = null, int? grupoId = null, string? periodo = null)
    {
        var sql = @"
            SELECT c.*,
                   a.Nombre || ' ' || a.ApellidoPaterno || ' ' || COALESCE(a.ApellidoMaterno,'') AS AlumnoNombre,
                   m.Nombre AS MateriaNombre,
                   g.Nombre AS GrupoNombre
            FROM Calificaciones c
            JOIN Alumnos  a ON c.AlumnoId  = a.Id
            JOIN Materias m ON c.MateriaId = m.Id
            JOIN Grupos   g ON c.GrupoId   = g.Id
            WHERE 1=1";

        var parametros = new Dictionary<string, object?>();

        if (alumnoId.HasValue)
        { sql += " AND c.AlumnoId = @AlumnoId"; parametros["@AlumnoId"] = alumnoId; }
        if (grupoId.HasValue)
        { sql += " AND c.GrupoId = @GrupoId"; parametros["@GrupoId"] = grupoId; }
        if (!string.IsNullOrWhiteSpace(periodo))
        { sql += " AND c.Periodo = @Periodo"; parametros["@Periodo"] = periodo; }

        sql += " ORDER BY a.ApellidoPaterno, m.Nombre";

        var filas = await _q.QueryAsync(sql, parametros);
        return filas.Select(MapearCalificacion).ToList();
    }

    public async Task<Calificacion?> ObtenerPorIdAsync(int id)
    {
        var sql = @"
            SELECT c.*, a.Nombre || ' ' || a.ApellidoPaterno AS AlumnoNombre,
                   m.Nombre AS MateriaNombre, g.Nombre AS GrupoNombre
            FROM Calificaciones c
            JOIN Alumnos  a ON c.AlumnoId  = a.Id
            JOIN Materias m ON c.MateriaId = m.Id
            JOIN Grupos   g ON c.GrupoId   = g.Id
            WHERE c.Id = @id";

        var filas = await _q.QueryAsync(sql, new() { ["@id"] = id });
        return filas.Count > 0 ? MapearCalificacion(filas[0]) : null;
    }

    public async Task<long> GuardarAsync(Calificacion cal)
    {
        // Calcula promedio antes de guardar
        cal.Promedio = Math.Round((cal.Parcial1 + cal.Parcial2 + cal.Parcial3) / 3, 2);
        cal.Estado   = cal.Promedio >= 6 ? "Aprobado" : (cal.Promedio > 0 ? "Reprobado" : "En Curso");

        var valores = new Dictionary<string, object?> {
            ["InscripcionId"]      = cal.InscripcionId,
            ["AlumnoId"]           = cal.AlumnoId,
            ["MateriaId"]          = cal.MateriaId,
            ["GrupoId"]            = cal.GrupoId,
            ["Parcial1"]           = cal.Parcial1,
            ["Parcial2"]           = cal.Parcial2,
            ["Parcial3"]           = cal.Parcial3,
            ["Promedio"]           = cal.Promedio,
            ["Estado"]             = cal.Estado,
            ["Periodo"]            = cal.Periodo,
            ["Observaciones"]      = cal.Observaciones,
            ["FechaActualizacion"] = DateTime.Now.ToString("o")
        };

        if (cal.Id == 0) return await _q.InsertarAsync("Calificaciones", valores);
        await _q.ActualizarAsync("Calificaciones", valores, new() { ["Id"] = cal.Id });
        return cal.Id;
    }

    public async Task EliminarAsync(int id) =>
        await _q.EliminarAsync("Calificaciones", new() { ["Id"] = id });

    public async Task<decimal> ObtenerPromedioGrupoAsync(int grupoId)
    {
        var r = await _q.EscalarAsync(
            "SELECT AVG(Promedio) FROM Calificaciones WHERE GrupoId = @gid AND Promedio > 0",
            new() { ["@gid"] = grupoId });
        return r is not null && r != DBNull.Value ? Math.Round(Convert.ToDecimal(r), 2) : 0;
    }

    public async Task<byte[]> ExportarExcelAsync(
        int? alumnoId = null, int? grupoId = null, string? periodo = null)
    {
        var lista = await ObtenerTodosAsync(alumnoId, grupoId, periodo);
        return excel.ExportarCalificaciones(lista);
    }

    public async Task<byte[]> ExportarBoletaAsync(int alumnoId)
    {
        var opciones = new QueryOptions {
            Filtros = [new QueryFilter { Campo = "Id", Operador = "=", Valor = alumnoId }]
        };
        var alumnoRows = await _q.EjecutarAsync("Alumnos", opciones);
        var nombre = alumnoRows.Count > 0
            ? $"{alumnoRows[0]["Nombre"]} {alumnoRows[0]["ApellidoPaterno"]}"
            : "Alumno";

        var calificaciones = await ObtenerTodosAsync(alumnoId: alumnoId);
        return excel.ExportarBoleta(nombre, calificaciones);
    }

    private static Calificacion MapearCalificacion(Dictionary<string, object?> row) => new()
    {
        Id                  = Convert.ToInt32(row["Id"]),
        InscripcionId       = Convert.ToInt32(row["InscripcionId"]),
        AlumnoId            = Convert.ToInt32(row["AlumnoId"]),
        MateriaId           = Convert.ToInt32(row["MateriaId"]),
        GrupoId             = Convert.ToInt32(row["GrupoId"]),
        AlumnoNombre        = row["AlumnoNombre"]?.ToString() ?? "",
        MateriaNombre       = row["MateriaNombre"]?.ToString() ?? "",
        GrupoNombre         = row["GrupoNombre"]?.ToString() ?? "",
        Parcial1            = Convert.ToDecimal(row["Parcial1"] ?? 0),
        Parcial2            = Convert.ToDecimal(row["Parcial2"] ?? 0),
        Parcial3            = Convert.ToDecimal(row["Parcial3"] ?? 0),
        Promedio            = Convert.ToDecimal(row["Promedio"] ?? 0),
        Estado              = row["Estado"]?.ToString() ?? "En Curso",
        Periodo             = row["Periodo"]?.ToString() ?? "",
        Observaciones       = row["Observaciones"]?.ToString() ?? "",
        FechaActualizacion  = DateTime.TryParse(row["FechaActualizacion"]?.ToString(), out var fa) ? fa : DateTime.Now
    };
}

// ══════════════════════════════════════════════════════════════════════════════
//  REPORTE SERVICE — agrega datos de múltiples servicios + exporta kardex
// ══════════════════════════════════════════════════════════════════════════════

public class ReporteService(
    DatabaseService db,
    AlumnoService alumnoSvc,
    CalificacionService calSvc,
    InscripcionService inscSvc,
    ExcelExportService excel)
{
    private readonly ObjectPatternQuery _q = db.Query;

    public async Task<ResumenDashboard> ObtenerResumenAsync()
    {
        var totalAlumnos     = await _q.EscalarAsync("SELECT COUNT(*) FROM Alumnos WHERE Activo=1");
        var totalMaestros    = await _q.EscalarAsync("SELECT COUNT(*) FROM Maestros WHERE Activo=1");
        var totalMaterias    = await _q.EscalarAsync("SELECT COUNT(*) FROM Materias WHERE Activo=1");
        var totalInscripciones = await _q.EscalarAsync("SELECT COUNT(*) FROM Inscripciones WHERE Estado='Activa'");
        var promedioGeneral  = await _q.EscalarAsync("SELECT AVG(Promedio) FROM Calificaciones WHERE Promedio > 0");
        var aprobados        = await _q.EscalarAsync("SELECT COUNT(*) FROM Calificaciones WHERE Estado='Aprobado'");
        var reprobados       = await _q.EscalarAsync("SELECT COUNT(*) FROM Calificaciones WHERE Estado='Reprobado'");

        return new ResumenDashboard {
            TotalAlumnos       = Convert.ToInt32(totalAlumnos),
            TotalMaestros      = Convert.ToInt32(totalMaestros),
            TotalMaterias      = Convert.ToInt32(totalMaterias),
            TotalInscripciones = Convert.ToInt32(totalInscripciones),
            PromedioGeneral    = promedioGeneral != null && promedioGeneral != DBNull.Value
                                    ? Math.Round(Convert.ToDecimal(promedioGeneral), 2) : 0,
            Aprobados          = Convert.ToInt32(aprobados),
            Reprobados         = Convert.ToInt32(reprobados),
        };
    }

    public async Task<List<PromedioGrupo>> ObtenerPromediosPorGrupoAsync()
    {
        var sql = @"
            SELECT g.Nombre AS Grupo, g.Grado,
                   COUNT(DISTINCT c.AlumnoId) AS TotalAlumnos,
                   ROUND(AVG(c.Promedio),2)   AS PromedioGrupo,
                   SUM(CASE WHEN c.Estado='Aprobado'  THEN 1 ELSE 0 END) AS Aprobados,
                   SUM(CASE WHEN c.Estado='Reprobado' THEN 1 ELSE 0 END) AS Reprobados
            FROM Grupos g
            LEFT JOIN Calificaciones c ON g.Id = c.GrupoId
            GROUP BY g.Id, g.Nombre, g.Grado
            ORDER BY g.Nombre";

        var filas = await _q.QueryAsync(sql);
        return filas.Select(r => new PromedioGrupo {
            Grupo        = r["Grupo"]?.ToString() ?? "",
            Grado        = r["Grado"]?.ToString() ?? "",
            TotalAlumnos = Convert.ToInt32(r["TotalAlumnos"] ?? 0),
            Promedio     = r["PromedioGrupo"] != null && r["PromedioGrupo"] != DBNull.Value
                            ? Convert.ToDecimal(r["PromedioGrupo"]) : 0,
            Aprobados    = Convert.ToInt32(r["Aprobados"] ?? 0),
            Reprobados   = Convert.ToInt32(r["Reprobados"] ?? 0)
        }).ToList();
    }

    public async Task<byte[]> ExportarKardexAsync()
    {
        var alumnos       = await alumnoSvc.ObtenerTodosAsync();
        var calificaciones = await calSvc.ObtenerTodosAsync();
        var inscripciones = await inscSvc.ObtenerTodosAsync();
        return excel.ExportarKardex(alumnos, calificaciones, inscripciones);
    }
}

// ── DTOs para reportes ────────────────────────────────────────────────────────

public class ResumenDashboard
{
    public int TotalAlumnos       { get; set; }
    public int TotalMaestros      { get; set; }
    public int TotalMaterias      { get; set; }
    public int TotalInscripciones { get; set; }
    public decimal PromedioGeneral { get; set; }
    public int Aprobados          { get; set; }
    public int Reprobados         { get; set; }
}

public class PromedioGrupo
{
    public string Grupo     { get; set; } = "";
    public string Grado     { get; set; } = "";
    public int TotalAlumnos { get; set; }
    public decimal Promedio { get; set; }
    public int Aprobados    { get; set; }
    public int Reprobados   { get; set; }
}
