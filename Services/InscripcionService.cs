using EscuelaApp.Models;

namespace EscuelaApp.Services
{
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
            var valores = new Dictionary<string, object?>
            {
                ["AlumnoId"] = inscripcion.AlumnoId,
                ["GrupoId"] = inscripcion.GrupoId,
                ["MateriaId"] = inscripcion.MateriaId,
                ["FechaInscripcion"] = inscripcion.FechaInscripcion.ToString("o"),
                ["Estado"] = inscripcion.Estado,
                ["CicloEscolar"] = inscripcion.CicloEscolar
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
            Id = Convert.ToInt32(row["Id"]),
            AlumnoId = Convert.ToInt32(row["AlumnoId"]),
            GrupoId = Convert.ToInt32(row["GrupoId"]),
            MateriaId = Convert.ToInt32(row["MateriaId"]),
            AlumnoNombre = row["AlumnoNombre"]?.ToString() ?? "",
            AlumnoMatricula = row["AlumnoMatricula"]?.ToString() ?? "",
            GrupoNombre = row["GrupoNombre"]?.ToString() ?? "",
            MateriaNombre = row["MateriaNombre"]?.ToString() ?? "",
            FechaInscripcion = DateTime.TryParse(row["FechaInscripcion"]?.ToString(), out var fi) ? fi : DateTime.Now,
            Estado = row["Estado"]?.ToString() ?? "Activa",
            CicloEscolar = row["CicloEscolar"]?.ToString() ?? ""
        };
    }
}
