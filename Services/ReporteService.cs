namespace EscuelaApp.Services;

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
        var totalAlumnos = await _q.EscalarAsync("SELECT COUNT(*) FROM Alumnos WHERE Activo=1");
        var totalMaestros = await _q.EscalarAsync("SELECT COUNT(*) FROM Maestros WHERE Activo=1");
        var totalMaterias = await _q.EscalarAsync("SELECT COUNT(*) FROM Materias WHERE Activo=1");
        var totalInscripciones = await _q.EscalarAsync("SELECT COUNT(*) FROM Inscripciones WHERE Estado='Activa'");
        var promedioGeneral = await _q.EscalarAsync("SELECT AVG(Promedio) FROM Calificaciones WHERE Promedio > 0");
        var aprobados = await _q.EscalarAsync("SELECT COUNT(*) FROM Calificaciones WHERE Estado='Aprobado'");
        var reprobados = await _q.EscalarAsync("SELECT COUNT(*) FROM Calificaciones WHERE Estado='Reprobado'");

        return new ResumenDashboard
        {
            TotalAlumnos = Convert.ToInt32(totalAlumnos),
            TotalMaestros = Convert.ToInt32(totalMaestros),
            TotalMaterias = Convert.ToInt32(totalMaterias),
            TotalInscripciones = Convert.ToInt32(totalInscripciones),
            PromedioGeneral = promedioGeneral != null && promedioGeneral != DBNull.Value
                                    ? Math.Round(Convert.ToDecimal(promedioGeneral), 2) : 0,
            Aprobados = Convert.ToInt32(aprobados),
            Reprobados = Convert.ToInt32(reprobados),
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
        return filas.Select(r => new PromedioGrupo
        {
            Grupo = r["Grupo"]?.ToString() ?? "",
            Grado = r["Grado"]?.ToString() ?? "",
            TotalAlumnos = Convert.ToInt32(r["TotalAlumnos"] ?? 0),
            Promedio = r["PromedioGrupo"] != null && r["PromedioGrupo"] != DBNull.Value
                            ? Convert.ToDecimal(r["PromedioGrupo"]) : 0,
            Aprobados = Convert.ToInt32(r["Aprobados"] ?? 0),
            Reprobados = Convert.ToInt32(r["Reprobados"] ?? 0)
        }).ToList();
    }

    public async Task<byte[]> ExportarKardexAsync()
    {
        var alumnos = await alumnoSvc.ObtenerTodosAsync();
        var calificaciones = await calSvc.ObtenerTodosAsync();
        var inscripciones = await inscSvc.ObtenerTodosAsync();
        return excel.ExportarKardex(alumnos, calificaciones, inscripciones);
    }
}
