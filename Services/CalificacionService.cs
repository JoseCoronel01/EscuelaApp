using EscuelaApp.Models;

namespace EscuelaApp.Services
{
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
            cal.Estado = cal.Promedio >= 6 ? "Aprobado" : (cal.Promedio > 0 ? "Reprobado" : "En Curso");

            var valores = new Dictionary<string, object?>
            {
                ["InscripcionId"] = cal.InscripcionId,
                ["AlumnoId"] = cal.AlumnoId,
                ["MateriaId"] = cal.MateriaId,
                ["GrupoId"] = cal.GrupoId,
                ["Parcial1"] = cal.Parcial1,
                ["Parcial2"] = cal.Parcial2,
                ["Parcial3"] = cal.Parcial3,
                ["Promedio"] = cal.Promedio,
                ["Estado"] = cal.Estado,
                ["Periodo"] = cal.Periodo,
                ["Observaciones"] = cal.Observaciones,
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
            var opciones = new QueryOptions
            {
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
            Id = Convert.ToInt32(row["Id"]),
            InscripcionId = Convert.ToInt32(row["InscripcionId"]),
            AlumnoId = Convert.ToInt32(row["AlumnoId"]),
            MateriaId = Convert.ToInt32(row["MateriaId"]),
            GrupoId = Convert.ToInt32(row["GrupoId"]),
            AlumnoNombre = row["AlumnoNombre"]?.ToString() ?? "",
            MateriaNombre = row["MateriaNombre"]?.ToString() ?? "",
            GrupoNombre = row["GrupoNombre"]?.ToString() ?? "",
            Parcial1 = Convert.ToDecimal(row["Parcial1"] ?? 0),
            Parcial2 = Convert.ToDecimal(row["Parcial2"] ?? 0),
            Parcial3 = Convert.ToDecimal(row["Parcial3"] ?? 0),
            Promedio = Convert.ToDecimal(row["Promedio"] ?? 0),
            Estado = row["Estado"]?.ToString() ?? "En Curso",
            Periodo = row["Periodo"]?.ToString() ?? "",
            Observaciones = row["Observaciones"]?.ToString() ?? "",
            FechaActualizacion = DateTime.TryParse(row["FechaActualizacion"]?.ToString(), out var fa) ? fa : DateTime.Now
        };
    }
}
